(in-package #:slxi)

(declaim (optimize (speed 0) (safety 3) (debug 3)))

(defvar *current-symbol* +slxi+)
(declaim (type sl-symbol *current-symbol*))

(defvar *current-aliases* (make-hash-table :test #'equal))
(declaim (type hash-table *current-aliases*))

(define-constant +null-char+ #.(code-char 0))
(define-constant +bell-char+ #.(code-char 7))
(define-constant +backspace-char+ #.(code-char 8))
(define-constant +tab-char+ #.(code-char 9))
(define-constant +nl-char+ #.(code-char 10))
(define-constant +vt-char+ #.(code-char 11))
(define-constant +ff-char+ #.(code-char 12))
(define-constant +return-char+ #.(code-char 13))
(define-constant +space-char+ #\space)

(declaim (inline ws-char-p))
(defun ws-char-p (c)
  (or (eql c +space-char+)
      (eql c +tab-char+)
      (eql c +nl-char+)
      (eql c +return-char+)))

(defun term-char-p (c)
  (or (null c)
      (ws-char-p c)
      (eql c #\()
      (eql c #\))
      (eql c #\;)
      (eql c #\,)
      (eql c #\')
      (eql c #\`)
      (eql c #\")))

(defun nl-char-p (c)
  (or (eql c +nl-char+)
      (eql c +return-char+)))

(defvar *lex-base* 10)
(declaim (type (integer 2 36) *lex-base*))

(defun base-char-p (c)
  (declare (type (or null character) c))
  (and c (digit-char-p c *lex-base*)))

(defun decimal-char-p (c)
  (declare (type (or null character) c))
  (and c (digit-char-p c 10)))

(defun expt-char-p (c)
  (declare (type (or null character) c))
  (case c ((#\e #\E #\s #\S #\d #\D #\l #\L #\f #\F) t)))

(defun lex-intern-char (token)
  (casep* token
    ("space" (values t #\space))
    ("tab" (values t +tab-char+))
    (("return" "ret") (values t +return-char+))
    (("newline" "linefeed" "lf") (values t +nl-char+))
    (("backspace" "bs") (values t +backspace-char+))
    (("null" "nul") (values t +null-char+))
    (("bell" "bel") (values t +bell-char+))
    (("feed" "formfeed" "ff") (values t +ff-char+))
    ("vt" (values t +vt-char+))
    (t (values nil (format nil "Unknown character name: '~a'"
                           token)))))

(defmacro with-lex-arith-trap (&body body)
  (let ((e (mksymbol '#:error)))
    `(handler-case
         (values t (progn ,@body))
       (arithmetic-error (,e)
         (values nil (format nil "~a" ,e))))))

(defun lex-find-or-intern (name)
  (declare (type string name))
  (let ((name (mkstring name)))
    (or (values (gethash name *current-aliases*))
        (loop :for this = *current-symbol*
              :then (sl-sym-parent this)
              :for child = (sl-sym-child this name)
              :until (or child
                         (eq this +t+)
                         (eq this +nil+))
              :finally
                 (return (or child
                             (sl-intern *current-symbol* name)))))))

(defun lex-intern-symbol (root parts)
  (declare (type (or sl-symbol (member t nil)) root)
           (type (or list string) parts))
  (mklistf parts)
  (setf parts (reverse parts))
  (case root
    ((nil) (setf root (make-sl-symbol :name (pop parts))))
    ((t) (setf root (lex-find-or-intern (pop parts)))))
  (loop :with this :of-type sl-symbol = root
        :for name :in parts :do
          (setf this (sl-intern this name))
        :finally
           (return this)))

(deflexer sl-lexer (lexer)
    (char token slice)
  ((*lex-base* 10)
   (int 0)
   (ratio 0)
   (radix 0)
   (radix-len 0)
   (expt 0)
   (sign 1)
   (expt-sign 1)
   (float 1.0d0)
   (base-sign 1)
   (base-int 0)
   (base-ratio 0)
   (root t)
   (names '()))
  :start
  (nil :token :eof)
  (ws-char-p :skip :start)
  (#\( :token* :open)
  (#\) :token* :close)
  (#\' :token* :quote)
  (#\` :token* :backquote)
  (#\, :next :comma)
  (#\" :go :string)
  (#\; :go :comment)
  (#\: :go :maybe-keyword)
  (#\# :go :sharpsign)
  ((#\- :next :sign)
   (setf sign -1))
  (#\+ :next :sign)
  (#\. :next :dot)
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  ((decimal-char-p :next :int)
   (setf int (digit-char-p char)))
  (t :next :symbol)
  :maybe-keyword
  (term-char-p :error "Symbol name expected.")
  (#\: :go :maybe-toplevel)
  ((#\\ :go :symbol-escape)
   (setf root +nil+))
  ((#\| :go :symbol-multi-escape)
   (setf root +nil+))
  ((t :next :symbol)
   (setf root +nil+))
  :maybe-toplevel
  (term-char-p :error "Symbol name expected.")
  (#\: :error "Too many colons.")
  ((#\\ :go :symbol-escape)
   (setf root +t+))
  ((#\| :go :symbol-multi-escape)
   (setf root +t+))
  ((t :next :symbol)
   (setf root +t+))
  :sharpsign
  (nil :error "End of file after sharpsign.")
  (#\< :error "Attempt to read unreadable object.")
  ((#\: :go :sharpsign-colon)
   (setf root nil))
  (#\( :token* :vopen)
  (#\| :go :multi-comment)
  (((#\b #\B) :go :base-rational)
   (setf *lex-base* 2))
  (((#\o #\O) :go :base-rational)
   (setf *lex-base* 8))
  (((#\d #\D) :go :base-rational)
   (setf *lex-base* 10))
  (((#\x #\X) :go :base-rational)
   (setf *lex-base* 16))
  (#\\ :go :char)
  (t :error "Invalid sharpsign dispatch character: '~a'." char)
  :sharpsign-colon
  (term-char-p :error "Symbol name expected.")
  (#\: :error "Too many colons")
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :multi-comment
  (nil :error "End of file inside multiline comment.")
  (#\| :go :multi-comment-vbar)
  (t :go :multi-comment)
  :multi-comment-vbar
  (nil :error "End of file inside multiline comment.")
  (#\# :skip :start)
  (t :go :multi-comment)
  :char
  (nil :error "Character name expected.")
  (term-char-p :token :char char)
  (t :next :char-name)
  :char-name
  (term-char-p :maybe-token :char (lex-intern-char token))
  (#\\ :go :char-escape)
  (#\| :go :char-multi-escape)
  (t :next :char-name)
  :char-escape
  (t :next :char-name)
  :char-multi-escape
  (#\| :go :char-name)
  (#\\ :go :char-inner-escape)
  (t :next :char-multi-escape)
  :char-inner-escape
  (t :go :char-multi-escape)
  :base-rational
  (ws-char-p :go :base-rational)
  ((#\- :go :base-sign)
   (setf base-sign -1))
  (#\+ :go :base-sign)
  ((base-char-p :go :base-int)
   (setf base-int (digit-char-p char *lex-base*)))
  (t :error "Rational (base ~a) expected." *lex-base*)
  :base-sign
  ((base-char-p :go :base-int)
   (setf base-int (digit-char-p char *lex-base*)))
  (t :error "Rational (base ~a) expected." *lex-base*)
  :base-int
  (term-char-p :token :number (* base-sign base-int))
  ((base-char-p :go :base-int)
   (setf base-int (+ (digit-char-p char *lex-base*)
                     (* base-int *lex-base*))))
  (#\/ :go :base-slash)
  (t :error "Rational (base ~a) expected." *lex-base*)
  :base-slash
  ((base-char-p :go :base-ratio)
   (setf base-ratio (digit-char-p char *lex-base*)))
  (t :error "Rational (base ~a) expected." *lex-base*)
  :base-ratio
  (term-char-p :token :number (* base-sign (/ base-int base-ratio)))
  ((base-char-p :go :base-ratio)
   (setf base-ratio (+ (digit-char-p char *lex-base*)
                       (* base-ratio *lex-base*))))
  (t :error "Rational (base ~a) expected." *lex-base*)
  :comma
  (#\@ :token* :unquote-list)
  (#\. :token* :unquote-splice)
  (t :token :unquote)
  :string
  (nil :error "End of file inside string literal.")
  (#\" :token! :string)
  (#\\ :go :string-escape)
  (t :next :string)
  :string-escape
  (nil :error "End of file inside string escape sequence.")
  (#\" :next :string)
  (#\\ :next :string)
  (#\t :append :string +tab-char+)
  (#\r :append :string +return-char+)
  (#\n :append :string +nl-char+)
  (#\0 :append :string +null-char+)
  (#\b :append :string +backspace-char+)
  (#\a :append :string +bell-char+)
  (#\v :append :string +vt-char+)
  (t :error "Invalid escape character.")
  :comment
  (nil :token :eof)
  (nl-char-p :skip :start)
  (t :go :comment)
  :dot
  (term-char-p :token :dot)
  ((decimal-char-p :next :radix)
   (setf radix (digit-char-p char)
         radix-len 1))
  (t :next :symbol)
  :sign
  (term-char-p :token :symbol (lex-intern-symbol root token))
  ((decimal-char-p :next :int)
   (setf int (+ (digit-char-p char) (* int 10))))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :int
  (term-char-p :token :number (* int sign))
  ((decimal-char-p :next :int)
   (setf int (+ (digit-char-p char) (* int 10))))
  (#\. :next :radix-dot)
  ((expt-char-p :next :expt)
   (case char
     ((#\s #\S #\f #\F) (setf float 1.0f0))))
  (#\/ :next :slash)
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :slash
  (term-char-p :token :symbol (lex-intern-symbol root token))
  ((decimal-char-p :next :ratio)
   (setf ratio (digit-char-p char)))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :ratio
  (term-char-p :token :number (* sign (/ int ratio)))
  ((decimal-char-p :next :ratio)
   (setf ratio (+ (digit-char-p char) (* ratio 10))))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :radix-dot
  (term-char-p :token :number (* sign int))
  ((decimal-char-p :next :radix)
   (setf radix (digit-char-p char)
         radix-len 1))
  ((expt-char-p :next :expt)
   (case char
     ((#\f #\F #\s #\S) (setf float 1.0f0))))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :radix
  (term-char-p :maybe-token :number
               (with-lex-arith-trap
                   (* sign
                      float
                      (+ int (* radix (expt 10 (- radix-len)))))))
  ((decimal-char-p :next :radix)
   (incf radix-len)
   (setf radix (+ (digit-char-p char)
                  (* 10 radix))))
  ((expt-char-p :next :expt)
   (case char
     ((#\f #\F #\s #\S) (setf float 1.0f0))))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :expt
  (term-char-p :token :symbol (lex-intern-symbol root token))
  (#\+ :next :expt-sign)
  ((#\- :next :expt-sign)
   (setf expt-sign -1))
  ((decimal-char-p :next :expt-value)
   (setf expt (digit-char-p char)))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :expt-sign
  (term-char-p :token :symbol (lex-intern-symbol root token))
  ((decimal-char-p :next :expt-value)
   (setf expt (digit-char-p char)))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :expt-value
  (term-char-p :maybe-token :number
               (with-lex-arith-trap
                   (* sign
                      float
                      (+ int (* radix (expt 10 (- radix-len))))
                      (expt 10 (* expt-sign expt)))))
  ((decimal-char-p :next :expt-value)
   (setf expt (+ (digit-char-p char)
                 (* expt 10))))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :colon
  (term-char-p :error "Symbol name expected.")
  (#\: :go :double-colon)
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :double-colon
  (term-char-p :error "Symbol name expected.")
  (#\: :error "Too many colons.")
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol)
  :symbol-escape
  (nil :error "End of file after escape character.")
  (t :next :symbol)
  :symbol-multi-escape
  (nil :error "End of file inside symbol escape sequence.")
  (#\| :go :symbol)
  (#\\ :go :symbol-inner-escape)
  (t :next :symbol-multi-escape)
  :symbol-inner-escape
  (nil :error "End of file after escape character.")
  (t :next :symbol-multi-escape)
  :symbol
  ((term-char-p :token :symbol (lex-intern-symbol root names))
   (push slice names))
  ((#\: :next-slice :colon)
   (push slice names))
  (#\\ :go :symbol-escape)
  (#\| :go :symbol-multi-escape)
  (t :next :symbol))

(defun tokenize-string (string)
  (with-input-from-string (in string)
    (let ((lex (%make-lexer :input in :filename "string")))
      (loop :for token = (sl-lexer lex)
            :until (eq :eof (tok-type token))
            :collect token))))

#|

Expr -> NUMBER
        / CHAR
        / STRING
        / SYMBOL
        / QUOTE Expr
        / BACKQUOTE Expr
        / UNQUOTE Expr
        / UNQUOTE_LIST Expr
        / UNQUOTE_SPLICE Expr
        / OPEN List
        / VOPEN Vector

List -> CLOSE
        / Expr ListRest

ListRest -> CLOSE
            / DOT Expr CLOSE
            / Expr ListRest

Vector -> CLOSE
          / Expr Vector
|#

(defstruct (sl-reader
            (:constructor %make-sl-reader)
            (:conc-name #:sl-reader-))
  (lexer (%make-lexer) :type lexer)
  (token (make-token) :type token))

(defun sl-reader-next-token (reader)
  (declare (type sl-reader reader))
  (setf (sl-reader-token reader)
        (sl-lexer (sl-reader-lexer reader))))

(defstruct (sl-parse-result
            (:constructor %make-sl-pr)
            (:conc-name #:sl-pr-))
  (line 1 :type index)
  (column 1 :type index)
  (filename (string '*standard-input*)
   :type (or pathname string))
  (error-p nil :type boolean)
  (value nil :type t))

(defun make-sl-pr (reader value &optional start-token error-p)
  (declare (type sl-reader reader))
  (%make-sl-pr :line (tok-line (or start-token
                                   (sl-reader-token reader)))
               :column (tok-column (or start-token
                                       (sl-reader-token reader)))
               :filename (lex-filename (sl-reader-lexer reader))
               :error-p error-p
               :value value))

(defun make-sl-pr* (reader value &optional start-token error-p)
  (declare (type sl-reader reader))
  (prog1 (make-sl-pr reader value start-token error-p)
    (unless error-p
      (sl-reader-next-token reader))))

(defun sl-parse-list-rest (reader)
  (declare (type sl-reader reader))
  (let ((token (sl-reader-token reader)))
    (case (tok-type token)
      (:close (make-sl-pr* reader +nil+))
      (:dot
       (sl-reader-next-token reader)
       (let ((expr (sl-parse-expr reader)))
         (if (sl-pr-error-p expr)
           expr
           (let ((token (sl-reader-next-token reader)))
             (case (tok-type token)
               (:eof (make-sl-pr reader "Close paren expected." t))
               (:error (make-sl-pr reader (tok-string token) t))
               (:close (make-sl-pr* reader (sl-pr-value expr)))
               (t (make-sl-pr reader
                              "More than one expression after dot."
                              t)))))))
      (t (let ((expr (sl-parse-expr reader)))
           (if (sl-pr-error-p expr)
             expr
             (let ((exprs (sl-parse-list-rest reader)))
               (if (sl-pr-error-p exprs)
                 exprs
                 (make-sl-pr reader (cons (sl-pr-value expr)
                                          (sl-pr-value exprs)))))))))))

(defun sl-parse-list (reader &optional (rest-fn #'sl-parse-list))
  (declare (type sl-reader reader)
           (type function rest-fn))
  (let ((token (sl-reader-token reader)))
    (case (tok-type token)
      (:close (make-sl-pr* reader +nil+))
      (t (let ((expr (sl-parse-expr reader)))
           (if (sl-pr-error-p expr)
             expr
             (let ((exprs (funcall rest-fn reader)))
               (if (sl-pr-error-p exprs)
                 exprs
                 (make-sl-pr reader (cons (sl-pr-value expr)
                                          (sl-pr-value exprs)))))))))))

(defun sl-parse-expr (reader)
  (declare (type sl-reader reader))
  (let ((token (sl-reader-token reader)))
    (ecase (tok-type token)
      ((nil) (make-sl-pr reader (tok-string token) token t))
      (:eof (make-sl-pr* reader nil token t))
      (:dot (make-sl-pr* reader "Invalid dot context." token t))
      (:close (make-sl-pr* reader "Unmatched close paren." token t))
      ((:number :char :symbol) (make-sl-pr* reader (tok-value token)))
      (:string (make-sl-pr* reader (tok-value token)))
      ((:quote :backquote :unquote :unquote-list :unquote-splice)
       (sl-reader-next-token reader)
       (let ((expr (sl-parse-expr reader)))
         (if (sl-pr-error-p expr)
           expr
           (make-sl-pr reader
                       (list* (case (tok-type token)
                                (:quote +quote+)
                                (:backquote +backquote+)
                                (:unquote +unquote+)
                                (:unquote-list +unquote-list+)
                                (:unquote-splice +unquote-splice+))
                              (sl-pr-value expr)
                              +nil+)
                       token))))
      (:vopen
       (sl-reader-next-token reader)
       (let ((exprs (sl-parse-list reader)))
         (if (sl-pr-error-p exprs)
           exprs
           (make-sl-pr reader
                       (sl-list-to-vector (sl-pr-value exprs))
                       token))))
      (:open
       (sl-reader-next-token reader)
       (let ((exprs (sl-parse-list reader #'sl-parse-list-rest)))
         (if (sl-pr-error-p exprs)
           exprs
           (make-sl-pr reader
                       (sl-pr-value exprs)
                       token)))))))
