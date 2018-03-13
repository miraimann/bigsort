### BigSort is solution for performance and architecture chelenging task.

***

### Task description:

   We have input file with lines (amount of lines cannot be zero).  
   Each line has the following format: **_number_._string_\n\r**. Where:
   * **_number_** is natural number with amount of digits that is less then **128**.  
   **_number_** cannot contain zeros in prefix except case when **_number_** **= 0**
   * **_string_** is ASCII printable characters sequence with length that is less than **256**.  

   Program must output new file with lines from input file, but ordered by **_string_** and then by **_number_**.  
   The input file is large. There is impossible to load it to memory fully.  
   Using of hard drive is only one way to sort it.  
   The solution must be implemented in both variants: library (dll) and console (exe).  
   Solution architecture must be SOLID and covered by unit and integration tests.  
   Performance has the highest priority.  

***

### Solution contains the following ojects:  
  1) **Bigsort.Contracts**
     is a project with all necessary contracts and primitives of program.
  2) **Bigsort.Implementation**
     is a project with all algorithms of program.
  3) **Bigsort.Lib**
     is a library part of solution. It provides inversion of control container.
     Bigsort.Lib contains single class with single static method Sort.
     Sort has two arguments **_input_** and **_output_** that are pathes of input file and of output file respectively.
  4) **Bigsort.Console**
     is an adapter for using Bigsort.Lib as console application.  
  5) **Bigsort.Tests**
     is a project with unit and integration tests.
  6) **Bigsort.Tools.TestFileGenerator**
     is console application tool for generating files with random lines valid for task description.
     It contains static class Generator with public method Generate.
     Also project is compiling to console application.
     It can be called with same arguments as method Generate.  
     Generate contains 3 string arguments:
        * **_sizeData_** sets size of output file. 
            It is string with folowing format **_size_unit_**. 
            Where:
              **_size_** is natural number
              **_unit_** has value **"b"** or **"Kb"** or **"Mb"** or **"Gb"**
        * **_lineSettings_** sets constraint for line format.
            It has following format **[_forNumber_].[_forString_]**.
            Where **_forNumber_** and **_forString_** sets length settings for number and string parts of lines.
            They can be one number **(123, 45)** to set concrete length 
            or range of valid length diapason with following format **_min_-_max_**, 
            where **_min_** is minimal and **_max_** is maximum of length of number or string part of line.
        * **_path_** is path for output file  
  7) **Bigsort.Tools.SortedFileChecker**
     is console application tool for checking that lines of file are sorted.
