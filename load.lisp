(in-package #:cl-user)

(declaim (optimize (speed 0) (safety 3) (debug 3)))

(flet ((load-file (name)
         (setf name (pathname name))
         (load
          (compile-file
           (make-pathname :defaults *load-pathname*
                          :name (pathname-name name)
                          :type (pathname-type name))))))
  (dolist (file '("package.lisp"
                  "utils.lisp"
                  "data.lisp"
                  "lexer.lisp"
                  "reader.lisp"
                  ))
    (load-file file)))
