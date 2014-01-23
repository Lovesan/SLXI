(in-package #:slxi)

(declaim (optimize (speed 0) (safety 3) (debug 3)))

(deftype index () '(mod #.array-total-size-limit))

(deftype simple-char-string () '(simple-array character (*)))

(defmacro defun-macro-helper (name (&rest args) &body body)
  `(eval-when (:compile-toplevel :load-toplevel :execute)
     (defun ,name ,args ,@body)))

(defun-macro-helper mklist (x)
  (if (listp x) x (list x)))

(define-modify-macro mklistf () mklist)

(defun-macro-helper mkstring (x)
  (etypecase x
    (string
     (coerce x 'simple-char-string))
    ((or symbol character)
     (coerce (string x) 'simple-char-string))))

(define-modify-macro mkstringf () mkstring)

(defun-macro-helper mksymbol (x)
  (make-symbol (mkstring x)))

(define-modify-macro mksymbolf () mksymbol)

(defun-macro-helper %reevaluate-constant (name value test)
  (if (boundp name)
    (let ((old-value (symbol-value name)))
      (if (funcall test old-value value)
        old-value
        value))
    value))

(defmacro define-constant (name value
                           &optional (test '(function equal)))
  `(eval-when (:compile-toplevel :load-toplevel :execute)
     (defconstant ,name (%reevaluate-constant ',name ,value ,test))))

(defmacro dolist* ((var list) &body body)
  `(mapcar (lambda (,var) ,@body) ,list))

(defmacro with-gensyms ((&rest symbols) &body body)
  (let ((bindings (dolist* (s symbols)
                    (if (atom s)
                      `(,s (gensym ,(mkstring s)))
                      `(,(first s)
                        (gensym ,(mkstring (second s))))))))
    `(let ,bindings
       ,@body)))

(defmacro %case* (value cmp &rest cases)
  (let ((var (gensym (string '#:var))))
    `(let ((,var ,value))
       (cond ,@(dolist* (c cases)
                 (etypecase (car c)
                   ((eql t) `(t ,@(cdr c)))
                   (list `((or ,@(dolist* (x (car c))
                                   `(,cmp ,x ,var)))
                           ,@(cdr c)))
                   (atom `((,cmp ,(car c) ,var) ,@(cdr c)))))))))

(defmacro case* (value &rest cases)
  `(%case* ,value equal ,@cases))

(defmacro casep* (value &rest cases)
  `(%case* ,value equalp ,@cases))

(defmacro with-collect (&body body)
  (with-gensyms (x list head)
    `(let* ((,list (cons nil nil))
            (,head ,list))
       (flet ((collect (,x)
                (setf (cdr ,list) (cons ,x nil)
                      ,list (cdr ,list))
                ,x))
         ,@body
         (cdr ,head)))))
