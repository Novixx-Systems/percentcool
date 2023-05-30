# Using percentCool
## What is percentCool?
percentCool (stylized with a lowercase p) is a programming language used to make websites.

This is the documentation for percentCool.

## How do I start?
To start, you need to run the percentCool executable. You can do this by running `./percentCool` in your terminal.

Running this will start a server on port 8000, and create the `www` and `sessions` directories.

## How do I make a website?
percentCool uses a file called `index.cool` to determine what to display on the website. This file needs to be created in the `www` directory.

Once you have created this file, you can start writing percentCool code, but since you haven't learned how to do that yet, you can just write HTML.

## How do I stop the server?
To stop the server, press `Ctrl+C` in your terminal.

# What are the data types?
percentCool has 2 data types: `string` and `array`.

## `string`
A `string` is a sequence of characters. It is created by surrounding text with double quotes (`"`).

You can also not use quotes at all, but then it will not parse spaces, meaning that `hello world` will be parsed as `hello`

## `array`
An `array` is a list of values. It is created by surrounding a list of values with `|` and each value with `,`.

For example, `|1,2,3|` is an array with the strings `1`, `2`, and `3`.

# Keywords
Oh, this will be a long one.

## echo
`echo` is used to print text to the page. It takes one argument, which is the text to print.

For example, `echo "Hello world!"` will print `Hello world!` to the page.

## rndmax
`rndmax` is used to set the maximum random number. It takes one argument, which is the maximum random number.

If no argument is given, it will return the default maximum random number.

For example, `rndmax 10` will set the maximum random number to 10.

## random
`random` is used to get a random number. It takes no arguments.

For example, `random` will return a random number in the variable `_RANDOM`.

## existing
`existing` is used to check if a variable exists. It takes one argument, which is the variable to check.

For example, `existing _RANDOM` will put `true` in the variable `_EXISTS` if `_RANDOM` exists, and `false` if it doesn't.

## escape
`escape` is used to escape a string for SQL. It takes one argument, which is the string to escape.

Once escaped, the string can be used in SQL queries. It is recommended to use this when using user input in SQL queries.

For example, `escape $name` will escape the string in the variable `name`

# I will update this later
