(in-package #:slxi)

(declaim (optimize (speed 0) (safety 3) (debug 3)))

(defstruct (sl-symbol
            (:conc-name #:sl-sym-))
  (name (mkstring "nil") :type simple-char-string)
  (value nil :type t)
  (parent nil :type (or null sl-symbol))
  (children (make-hash-table :test #'equal)
   :type hash-table)
  (keyword nil :type boolean))

(defun sl-sym-child (symbol name)
  (declare (type sl-symbol symbol)
           (type string name))
  (let ((name (mkstring name))
        (children (sl-sym-children symbol)))
    (values (gethash name children))))

(defvar +nil+ (make-sl-symbol :name (mkstring "nil")))
(defvar +t+ (make-sl-symbol :name (mkstring "t")))

(setf (sl-sym-parent +t+) +t+
      (sl-sym-parent +nil+) +t+
      (gethash "t" (sl-sym-children +t+)) +t+
      (gethash "nil" (sl-sym-children +t+)) +nil+
      (sl-sym-keyword +nil+) t
      (sl-sym-value +nil+) +nil+)

(defun sl-intern (parent name)
  (declare (type sl-symbol parent)
           (type string name))
  (let ((name (mkstring name))
        (children (sl-sym-children parent)))
    (values
     (or (gethash name children)
         (let ((new (make-sl-symbol :name name
                                    :parent parent)))
           (when (sl-sym-keyword parent)
             (setf (sl-sym-keyword new) t
                   (sl-sym-value new) new))
           (setf (gethash name children) new))))))

(defun sl-keyword (name)
  (declare (type string name))
  (sl-intern +nil+ name))

(defun sl-std-sym (name)
  (declare (type string name))
  (sl-intern +t+ name))

(defmethod print-object ((s sl-symbol) stream)
  (print-unreadable-object (s stream :type t)
    (cond ((eq s +t+) (format stream "t"))
          ((eq s +nil+) (format stream "nil"))
          (t (loop :for this = s :then (sl-sym-parent this)
                   :until (or (null this)
                              (eq this +nil+)
                              (eq this +t+))
                   :collect (sl-sym-name this) :into names
                   :finally (format stream "~a~{~a~^:~} "
                                    (cond ((null this) "#:")
                                          ((eq this +t+) "::")
                                          (T ":"))
                                    (nreverse names)))))))

(defvar +slxi+ (sl-std-sym "slxi"))
(defvar +quote+ (sl-std-sym "quote"))
(defvar +backquote+ (sl-std-sym "backquote"))
(defvar +unquote+ (sl-std-sym "unquote"))
(defvar +unquote-list+ (sl-std-sym "unquote-list"))
(defvar +unquote-splice+ (sl-std-sym "unquote-splice"))

(declaim (inline sl-list* sl-cons sl-car sl-cdr sl-list-p sl-endp))

(defmacro with-sl-collect (&body body)
  (let ((list (gensym))
        (q (gensym))
        (x (gensym)))
    `(let* ((,list (cons nil +nil+))
            (,q ,list))
       (declare (ignorable ,list)
                (type cons ,list ,q))
       (flet ((sl-collect (,x)
                (setf (cdr ,list) (cons ,x +nil+)
                      ,list (cdr ,list))
                ,x))
         ,@body
         (cdr ,q)))))

(defun sl-list (&rest elements)
  (declare (dynamic-extent elements))
  (with-sl-collect
    (loop :for e :in elements :do (sl-collect e))))

(defun sl-list* (element &rest elements)
  (apply #'list* element elements))

(defun sl-cons (car cdr)
  (cons car cdr))

(defun sl-list-p (x)
  (or (consp x)
      (eq x +nil+)))

(defun sl-endp (list)
  (cond ((eq list +nil+) t)
        ((consp list) nil)
        (t (error "Object is not a list: ~s" list))))

(defun sl-car (list)
  (if (sl-endp list)
    +nil+
    (car list)))

(defun sl-cdr (list)
  (if (sl-endp list)
    +nil+
    (cdr list)))

(defun sl-nthcdr (list n)
  (declare (type index n))
  (loop :for i :below n
        :and sl-cons = list :then (sl-cdr sl-cons)
        :finally (return sl-cons)))

(defun sl-nth (list n)
  (declare (type index ))
  (sl-car (sl-nthcdr list n)))

(defmacro sl-dolist ((var list &optional (return-form '+nil+))
                     &body body)
  (let ((list-var (gensym)) (tag (gensym)))
    `(let ((,list-var ,list))
       (prog ()
          ,tag
          (if (sl-endp ,list-var)
            (return ,return-form))
          (let ((,var (car ,list-var)))
            ,@body
            (setf ,list-var (cdr ,list-var))
            (go ,tag))))))

(defun sl-list-to-list (list)
  (with-collect
    (sl-dolist (x list)
      (collect x))))

(defun list-to-sl-list (list)
  (with-sl-collect
    (dolist (x list)
      (sl-collect x))))

(defun sl-list-to-vector (list)
  (let ((v (make-array 0 :adjustable t :fill-pointer t)))
    (sl-dolist (x list (coerce v 'simple-vector))
      (vector-push-extend x v))))

(defun vector-to-sl-list (vector)
  (with-sl-collect
    (loop :for e :across vector
          :do (sl-collect e))))

(defun sl-mapcar (function list &rest more-lists)
  (declare (type function function))
  (with-sl-collect
    (if (endp more-lists)
      (loop :for x :on list
            :do (sl-collect (funcall function (sl-car x)))
            :finally (sl-endp (cdr x)))
      (loop :for lists = (cons list more-lists)
              :then (mapcar #'sl-cdr lists)
            :until (find +nil+ lists)
            :do (sl-collect (apply function (mapcar #'sl-car lists)))))))

(defun sl-maplist (function list &rest more-lists)
  (declare (type function function))
  (with-sl-collect
    (if (endp more-lists)
      (sl-dolist (x list)
        (sl-collect x))
      (loop :for lists = (cons list more-lists)
              :then (mapcar #'sl-cdr lists)
            :until (find +nil+ lists)
            :do (sl-collect (apply function lists))))))

(defvar *sl-match-vars* '())

(defun make-sl-match-list-case (var spec)
  (cond ((null spec)
         `(eq ,var +nil+))
        ((eq t spec) t)
        ((consp spec)
         (let ((elt (car spec)))           
           (cond ((eq elt :rest)
                  (unless (endp (cdr spec))
                    (error ":rest spec must be last"))
                  `(listp ,var))
                 ((and (consp elt)
                       (eq (car elt) :rest))
                  (unless (endp (cdr spec))
                    (error ":rest spec must be last"))
                  (push (second elt) *sl-match-vars*)
                  `(progn (setf ,(second elt) ,var)
                          (sl-list-p ,(second elt))))
                 (t (let ((head-var (gensym))
                          (tail-var (gensym)))
                      (push head-var *sl-match-vars*)
                      (push tail-var *sl-match-vars*)
                      `(progn (setf ,head-var (car ,var)
                                    ,tail-var (cdr ,var))
                              (and ,(make-sl-match-case
                                     head-var elt)
                                   ,(make-sl-match-list-case
                                     tail-var (rest spec)))))))))
        (t `(equal ,var ',spec))))

(defun make-sl-match-case (var spec)
  (mklistf spec)
  (case (car spec)
    ((t) t)
    (:predicate
     `(funcall ,(second spec) ,var))
    (:eq
     `(eq ,(second spec) ,var))
    (:equal
     `(equal ,(second spec) ,var))
    (:binding
     (let ((bvar (second spec)))
       (push bvar *sl-match-vars*)
       `(progn (setf ,bvar ,var)
               ,(make-sl-match-case bvar (third spec)))))
    (:list `(and (sl-list-p ,var)
                 ,(make-sl-match-list-case var (rest spec))))
    (:rest (error ":rest out of list spec"))
    (t `(equal ,var ',(car spec)))))

(defun make-sl-match (var block spec body)
  (let* ((*sl-match-vars* '())         
         (predicate-code (make-sl-match-case var spec)))
    `(let ,*sl-match-vars*
       (declare (ignorable ,@*sl-match-vars*))
       (when ,predicate-code
         (return-from ,block (progn ,@body))))))

(defmacro sl-match (value &rest cases)
  (let ((block-name (gensym))
        (var (gensym)))
    `(block ,block-name
       (let ((,var ,value))
         (declare (ignorable ,var))
         ,@(mapcar (lambda (spec)
                     (make-sl-match
                      var block-name (car spec) (cdr spec)))
                   cases)))))
