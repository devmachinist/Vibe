# Xavier Framework

The Xavier Framework is a powerful framework designed to facilitate the development of web applications by providing support for multiple programming languages, including C#, Python, and JavaScript. It allows you to define and process template strings in both C# and JavaScript, ensuring seamless integration and efficient development. Xavier also offers server-side rendering capabilities, enabling full-page rendering for enhanced performance and SEO.

## Table of Contents

- [Installation](#installation)
- [Usage](#usage)
  - [Template Syntax](#template-syntax)
  - [Creating a Component](#creating-a-component)
  - [Server-Side Rendering](#server-side-rendering)
- [Initialization](#initialization)
- [Examples](#examples)
- [Directives](#directives)
- [Information](#information)
- [Contributing](#contributing)
- [License](#license)

## Installation

To use Xavier, follow these steps using the cli:

- dotnet add package Devmachinist.Xavier
- or dotnet add package Devmachinist.Xavier.AOT
## Usage

### Template Syntax

The Xavier Framework recognizes three types of languages:

- `x{ ... }x` - This syntax denotes a C# execution. Any code within these tags will be interpreted as C# code.
- `{{ ... }}` - This syntax represents as JavaScript in a script tag. Code within these tags will be interpreted as JavaScript code.
- `py{ ... }py` - This syntax represents as python 3.4 IronPython. Code within these tags will be interpreted as python code.

### Creating a Component

To create a component using the Xavier Framework, follow these steps:

1. Create a new file with the `.xavier` extension (e.g., `MyComponent.xavier`).
2. Define your component within the `.xavier` file using the appropriate afformentioned syntax.
3. Implement the code behind for the component in a separate file with the same name as the component and a `.xavier.cs` extension (e.g., `MyComponent.xavier.cs`). Use the base class provided by Xavier Framework and match it with the component's code behind file.

### Server-Side Rendering

Xavier Framework supports server-side rendering, which provides benefits like improved performance and search engine optimization (SEO). To enable server-side rendering for Xavier pages, follow these steps:

1. Configure your web application to use Xavier Framework.
2. Use the `app.MapCsxNodes` method in your application's configuration to map the Xavier nodes to specific routes. This method specifies the URL pattern and the destination directory for Xavier pages.
3. Set the static fallback for Xavier:

```csharp
var memory = new Xavier.Memory();
memory.StaticFallback("c:/wwwroot/index.html");

//app build
app.MapCsxNodes("{controller=Home}/{action=Index}/{id?}", Environment.CurrentDirectory + "/Pages", memory);


## Initialization

To initialize Xavier in a .NET app, follow these steps:

1. Import the required namespace:

```csharp
using Xavier;
using Xavier.AOT;
```

 Call the `Init` method to initialize Xavier with the desired parameters. This method builds your assembly into the specified destination. The last part of the destination path should have a `.js` extension.

```csharp
var memory = new Xavier.Memory();

await memory.Init(root, destination, assembly, isSPATrue);
```

Or with AOT, pass in your memory object without calling memory.Init()...

```csharp
var memory = new Xavier.Memory();
Parallel.Invoke(async () =>
    {
     await aot.Init(
                    memory,
                    Environment.CurrentDirectory,
                    Environment.CurrentDirectory + "/wwwroot/Xavier",
                    null,
                    typeof(Program).Assembly
                    );
     });
```

## Examples

Here's an example of a `.xavier` file that demonstrates the usage of template strings in C# and JavaScript within the Xavier Framework:

```html
<!--This file should be named MyComponent.xavier -->

<div id="${this.target}auth">
</div>

{{
let username = "";
var target = '${this.target}'



}}

x{ 
// C# Code here

    var Items = new[]{"item1","item2","item3"};
    @foreach( var k in items){
        <div>
        @k
        </div>
    }
}x

```

Here is the code behind required for each component.

```csharp
//this file should be named MyComponent.xavier.cs
using Xavier;

namespace MyNamespace{
    public partial class MyComponent : CsxNode
    {
        new public bool? ShouldRender = true;
        public MyComponent(CsxNode xavier) : base(xavier){
        }
        public MyComponent(){
        }
    }
}
```
## Directives
Xavier 8.0 and onward has a new feature called directives. You don't want to have multiple directives within a single element. Instead, only use one directive per element. Here are some examples of how they work.
- Each
```
<div>
  -[#each array]
    <div> -[x.name] </div>
    <div> -[x.description] </div>
  -[/]
</div>
{{
  //This is how you retrieve your component...
  var component = window['${this.xid}'];

  //This is how you access the array
  component.array.push({name:"foo",description:"This is a description for the object named foo"});

  //To remove all items and bound nodes in the array from the page do the following
  component.array.splice(0, component.array.length);

  //x always is the representation of the object. You will always use 'x' to target the object of the array, as shown.
}}
```
The each directive stores and maintains the element. If your 'each' directive is created out of sequence on navigation(usually in an spa) then a new array will be created.
Components themselves are stored as well so that they maintain state. Arrays or lists in the c# code behind must be used to initialize this directive. A standard array does not work.

This directive uses a Xavier specific class called an ObservableArray that extends the Array type. This observable array creates a shadow that represents the elements on the page. So if you use 'splice()' or 'push' on the array, the nodes of the page reflect those changes without retargeting the parent element.

- If
```
<button onclick="toggleMe()">toggle</button>
<div>
-[#if someBool]
   <div>This bool is now true</div>
-[/]
</div>
{{
  //This is how you retrieve your component...
  var component = window['${this.xid}'];

  window.toggleMe = () => {
    component.someBool = !component.someBool;
  }

}}
```
The 'if' directive acts just as you would expect.
- Switch/Case
```
<div>
  -[#switch option]
    -[#case "option1"]
      <div>This is option 1</div>
    -[#case "option2"]
      <div>This is option 2</div>
    -[#case "option3"]
      <div>This is option 3</div>
    -[#default]
      <div>This is the default</div>
  -[/]
</div>

{{
  //This is how you retrieve your component...
  var component = window['${this.xid}'];

  component.option = "None of the above"; // returns the default option
}}
```
The 'switch' directive has only been tested with strings, but like all of these directives, may evolve in the future.
- Variable
```
 <input id="titleInput" onchange="updateTitle(event)" type="text"/>
 <div>
  -[title]
 </div>
 <div>
  -[content]
 </div>
 {{
  //This is how you retrieve your component...
  var component = window['${this.xid}'];

  window.updateTitle = (e) => {
    component.title = e.target.value;
  }
 }}
```
Xavier is always diffing the changes to all values within its components. If a change is detected it will update. Functions are ignored.
- onUpdate()
You can make changes using the onUpdate method built into each component.
```
<div id="updated">
</div>
{{
  //This is how you retrieve your component...
  var component = window['${this.xid}'];

  component.onUpdate = () => {
    console.log("${this.name} just updated")
    document.getElementById("updated").innerHTML = "You changed me!"
  }
}}
```

All directives (excluding the variable directive) use -[/] as a closure. The examples shown above reflect that. Have fun with this new feature and let us know if you have any issues.
## Information

- Xavier is experimental and should be treated as such.
- Known to cause thread pool starvation while using Devmachinist.Xavier.AOT . Simply stop the app when done testing and use the memory.Init(root,destination,assembly, isSPAbool) for production.

## Contributing

We welcome contributions from the community to enhance the Xavier Framework. If you find any issues, have suggestions for improvements, or would like to add new features, please submit a pull request.

## License

The Xavier Framework is released under the [MIT License](https://opensource.org/licenses/MIT). Feel free to use, modify, and distribute it according to the terms of this license.
