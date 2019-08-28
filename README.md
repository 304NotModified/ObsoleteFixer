# Obsolete Fixer :sparkles:

Fixer for `[Obsolete]` in C# with the powers of Roslyn! :gem:

Currently supported:

- Method calls
- Getters
- Setters
- contructor calls: `new MyClass` - no transformation of parameters yet
- static method, getters or setter calls and change type.

Arguments values could also be transformed, see examples.

![image](https://user-images.githubusercontent.com/5808377/63803023-39565980-c914-11e9-8287-5bdc096de1fd.png)




# Message syntax
To unleash the powers of this fixer, the text in the `[Obsolete]` needs to be in a recognisable format. 

The *replace value* needs to between backticks (`` ` ``) and after the text **Replace with** (case insensitive). Text before and after the "Replace with" is OK.

Examples OK:

- :+1: ``[Obsolete("Replace with `MyNewMethod`")]``
- :+1: ``[Obsolete("Replace with `MyNewMethod(x, y)`")]``
- :+1: ``[Obsolete("Replace with: `MyNewMethod`")]``
- :+1: ``[Obsolete("This method will be replaced! Replace with: `MyNewMethod`. This will be removed in version 123")]``

Examples nope:

- :-1:  `[Obsolete("Replace with 'MyNewMethod'")]` - no backticks

# Examples

## Simple method replacement

Given:
```c#
class MyClass
{

    [Obsolete("Replace with `MyNewMethod`")]
    public void MyOldMethod(string x, object y)
    {

    }

    public void MyNewMethod(string x2, object y2)
    {

    }
}
```
and call
```c#
myClass.MyOldMethod("text", 2);
```

Will be after the fix:
```c#
myClass.MyNewMethod("text", 2);
```

## Method replacement with argument transformations

Gives
```c#
class MyClass
{

    [Obsolete("Replace with `MyNewMethod(y, x, \"text2\")`")]
    public void MyOldMethod(string x, object y)
    {

    }

    public void MyNewMethod(object y, string x, string y2)
    {

    }
}
```
Note the argument's order has been changed and a new argument has been added! :grinning:

When having this call:
```c#
myClass.MyOldMethod("text", 2);
```

Will be after the fix:
```c#
myClass.MyNewMethod(2, "text", "text2");
```

# FAQ

### Why do I need this?
Because fixing obsolete code is boring! :sleeping: :sleeping:

### Why is the syntax so strict?
I prefer that the code could still be compiled after the fix. :sunglasses:

### Will the syntax be configurable?
Not sure, also not sure how that works with the VISX

### Why all those emojis :open_mouth: ?

:satisfied: :see_no_evil:


# Roadmap

- Inheritance: Support for changing base class / interface
- Constructor with parameter transformation
- Transformation of parameter type? (e.g. int to string)
