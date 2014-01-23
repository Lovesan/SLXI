(in-package #:slxi)

(declaim (optimize (speed 0) (safety 3) (debug 3)))

(defconstant +default-token-size+ 16)

(defstruct (lexer
            (:constructor %make-lexer)
            (:conc-name #:lex-))
  (input *standard-input* :type stream)
  (filename (mkstring '*standard-output*)
   :type simple-char-string)
  (line 1 :type index)
  (column 1 :type index)
  (tab-size 8 :type (member 2 4 8))
  (seen-cr nil :type boolean)
  (char nil :type (or null character))
  (peek nil :type boolean)
  (token (make-string +default-token-size+ :element-type 'character)
   :type simple-char-string)
  (token-size 0 :type index)
  (slice-index 0 :type index))

(defstruct (token
            (:conc-name #:tok-))
  (type nil :type symbol)
  (string (make-string 0 :element-type 'character)
   :type simple-char-string)
  (line 1 :type index)
  (column 1 :type index)
  (value nil :type t))

(defun lex-read (lexer)
  (with-accessors ((char lex-char)
                   (peek lex-peek)
                   (in lex-input))
      lexer
    (if peek
      char
      (let ((c (read-char in nil nil)))        
        (setf peek t)
        (setf char c)))))

(defun lex-next (lexer)
  (with-accessors ((char lex-char)
                   (line lex-line)
                   (column lex-column)
                   (seen-cr lex-seen-cr)
                   (peek lex-peek)
                   (tab-size lex-tab-size))
      lexer
    (case char
      (#\tab
       (setf column (logand (+ column (1- tab-size))
                            (lognot (1- tab-size)))
             seen-cr nil))
      (#\return
       (setf column 1
             seen-cr t)
       (incf line))
      (#\newline
       (unless seen-cr
         (setf column 1)
         (incf line))
       (setf seen-cr nil))
      (t
       (setf seen-cr nil)
       (incf column)))
    (setf peek nil)))

(defun lex-append (lexer char)
  (with-accessors ((token lex-token)
                   (token-size lex-token-size))
      lexer
    (unless (< token-size (length token))
      (let ((new-token (make-string (* 2 (length token))
                                    :element-type 'character)))
        (replace new-token token)
        (setf token new-token)))
    (setf (elt token token-size) char)
    (incf token-size)
    (values)))

(defun-macro-helper lex-p->cond-p (lex-var pred-spec)
  (etypecase pred-spec
    (null `(null (lex-char ,lex-var)))
    ((eql t) 't)
    (symbol `(,pred-spec (lex-char ,lex-var)))
    (character `(eql ,pred-spec (lex-char ,lex-var)))
    (list `(or ,@(dolist* (c pred-spec)
                   `(eql ,c (lex-char ,lex-var)))))))

(defun-macro-helper lex-case
    (lex-var line-var column-var token-var slice-var case)
  (let* ((spec (if (atom (car case))
                 case
                 (car case)))
         (code (if (atom (car case))
                 '()
                 (cdr case)))
         (ok-var (mksymbol '#:ok))
         (val-var (mksymbol '#:value)))
    (destructuring-bind
      (pred-spec action param &rest args) spec
      (let* ((pred (lex-p->cond-p lex-var pred-spec)))
        (case action
          (:go `(when ,pred
                  ,@code
                  (lex-next ,lex-var)
                  (go ,param)))
          (:skip `(when ,pred
                     ,@code
                     (lex-next ,lex-var)
                     (setf ,line-var (lex-line ,lex-var)
                           ,column-var (lex-column ,lex-var))
                     (go ,param)))
          (:next `(when ,pred
                    ,@code
                    (lex-append ,lex-var (lex-char ,lex-var))
                    (lex-next ,lex-var)
                    (go ,param)))
          (:go-slice `(when ,pred
                        (setf ,slice-var
                              (subseq (lex-token ,lex-var)
                                      (lex-slice-index ,lex-var)
                                      (lex-token-size ,lex-var))
                              (lex-slice-index ,lex-var)
                              (lex-token-size ,lex-var))
                        ,@code
                        (lex-next ,lex-var)
                        (go ,param)))
          (:next-slice `(when ,pred
                          (setf ,slice-var
                                (subseq (lex-token ,lex-var)
                                        (lex-slice-index ,lex-var)
                                        (lex-token-size ,lex-var))
                                (lex-slice-index ,lex-var)
                                (1+ (lex-token-size ,lex-var)))
                          ,@code
                          (lex-append ,lex-var (lex-char ,lex-var))
                          (lex-next ,lex-var)
                          (go ,param)))
          (:next-slice* `(when ,pred
                           (lex-append ,lex-var (lex-char ,lex-var))
                           (setf ,slice-var
                                 (subseq (lex-token ,lex-var)
                                         (lex-slice-index ,lex-var)
                                         (lex-token-size ,lex-var))
                                 (lex-slice-index ,lex-var)
                                 (lex-token-size ,lex-var))
                           ,@code
                           (lex-next ,lex-var)
                           (go ,param)))
          (:append `(when ,pred
                      ,@code
                      (lex-append ,lex-var (progn ,@args))
                      (lex-next ,lex-var)
                      (go ,param)))
          (:error `(when ,pred
                     ,@code
                     (return-from ,lex-var
                       (make-token
                        :type nil
                        :string (format nil ,param ,@args)
                        :line (lex-line ,lex-var)
                        :column (lex-column ,lex-var)))))
          (:maybe-token `(when ,pred
                           (setf ,slice-var
                                (subseq (lex-token ,lex-var)
                                        (lex-slice-index ,lex-var)
                                        (lex-token-size ,lex-var))
                                (lex-slice-index ,lex-var)
                                (lex-token-size ,lex-var))
                           (setf ,token-var
                                 (subseq (lex-token ,lex-var)
                                         0
                                         (lex-token-size ,lex-var)))
                           (multiple-value-bind
                                 (,ok-var ,val-var) (progn ,@args)
                             ,@code
                             (return-from ,lex-var
                               (if ,ok-var
                                 (make-token
                                  :type ,param
                                  :string ,token-var
                                  :line ,line-var
                                  :column ,column-var
                                  :value ,val-var)
                                 (make-token
                                  :type nil
                                  :string (format nil ,val-var)
                                  :line (lex-line ,lex-var)
                                  :column (lex-column ,lex-var)))))))
          (:token `(when ,pred
                     (setf ,slice-var
                                (subseq (lex-token ,lex-var)
                                        (lex-slice-index ,lex-var)
                                        (lex-token-size ,lex-var))
                                (lex-slice-index ,lex-var)
                                (lex-token-size ,lex-var))
                     (setf ,token-var (subseq (lex-token ,lex-var)
                                              0
                                              (lex-token-size ,lex-var)))
                     ,@code             
                     (return-from ,lex-var
                       (make-token
                        :type ,param
                        :string ,token-var
                        :line ,line-var
                        :column ,column-var
                        :value (progn ,@args)))))
          (:token! `(when ,pred
                      (setf ,slice-var
                            (subseq (lex-token ,lex-var)
                                    (lex-slice-index ,lex-var)
                                    (lex-token-size ,lex-var))
                            (lex-slice-index ,lex-var)
                            (lex-token-size ,lex-var))                      
                      (setf ,token-var (subseq (lex-token ,lex-var)
                                               0
                                               (lex-token-size ,lex-var)))
                      ,@code                      
                      (lex-next ,lex-var)                     
                      (return-from ,lex-var
                        (make-token
                         :type ,param
                         :string ,token-var
                         :line ,line-var
                         :column ,column-var
                         :value (progn ,@args)))))
          (:token* `(when ,pred
                      (lex-append ,lex-var (lex-char ,lex-var))
                      (setf ,slice-var
                            (subseq (lex-token ,lex-var)
                                    (lex-slice-index ,lex-var)
                                    (lex-token-size ,lex-var))
                            (lex-slice-index ,lex-var)
                            (lex-token-size ,lex-var))                      
                      (setf ,token-var (subseq (lex-token ,lex-var)
                                               0
                                               (lex-token-size ,lex-var)))
                      ,@code                      
                      (lex-next ,lex-var)                     
                      (return-from ,lex-var
                        (make-token
                         :type ,param
                         :string ,token-var
                         :line ,line-var
                         :column ,column-var
                         :value (progn ,@args))))))))))

(defmacro deflexer (name
                    (lexer &rest args)
                    (&optional (char-var (mksymbol '#:char))
                               (token-var (mksymbol '#:token))
                               (slice-var (mksymbol '#:slice)))
                    (&rest vars) &body body)
  (with-gensyms (line column)
    `(defun ,name (,lexer ,@args)
       (block ,lexer
         (let ((,char-var nil)
               (,token-var (lex-token ,lexer))
               (,slice-var (lex-token ,lexer))
               (,line (lex-line ,lexer))
               (,column (lex-column ,lexer))
               ,@vars)
           (setf (lex-token-size ,lexer) 0
                 (lex-slice-index ,lexer) 0)
           (tagbody
              ,@(loop :for s :in body
                      :append
                      (if (atom s)
                        `(,s (setf ,char-var (lex-read ,lexer)))
                        `(,(lex-case
                            lexer line column token-var slice-var s))))))))))
