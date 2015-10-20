// include plug-ins
var gulp = require('gulp');
var tslint = require('gulp-tslint');
 
gulp.task('tslint', function(){
      return gulp.src(['src/*.ts', 'src/*.tsx'])
        .pipe(tslint())
        .pipe(tslint.report('verbose', { emitError: false }));
});

gulp.task('default', ['tslint'], function () { });
