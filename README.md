# Vibe

Vibe is a revolutionary framework for building modern applications using `.csx` files. It combines the expressive power of C# with a JSX-like syntax to deliver an elegant, component-based approach to web and desktop development. With Vibe, you can define routes, components, and services all in one place, allowing you to create dynamic and maintainable applications effortlessly.

## Why Choose Vibe?

- **Unified Development**: Leverage the full power of C# in `.csx` files with JSX-like syntax for clean and intuitive code.
- **Dynamic Routing**: Seamlessly define routes and APIs within your application.
- **Service-Driven Architecture**: Register and inject services with ease, enabling modular and scalable applications.
- **Node module Integration**: Use `node_modules` to extend functionality with csx libraries.
- **Cross-Platform Compatibility**: Integrates smoothly with .NET MAUI for web and mobile/desktop applications.
- **Developer Productivity**: Focus on building features instead of boilerplate with Vibe's streamlined architecture.

---

## Installation

Add Vibe to your project via the .NET CLI:

```bash
dotnet add package Vibe
```

---

## How It Works

### Component-Based Development

Write your components and routes directly in `.csx` files. For example:

```cs

@inject ViewService ViewService;

import Home from "../Pages/Home.csx";
import About from "../Pages/About.csx";

export dynamic Router() {
    ViewService.AddViews([
        <div Route="/" View={Home()} />,
        <div Route="/About" View={About()} />
    ]);
    ViewService.SetFallbackView(Home());
    
    return <div class="w-full h-full">{ViewService}</div>;
}
```

### Integrated Server

Vibe includes an integrated server (`XServer`) for serving your application and APIs:

```cs

import {Layout} from "./src/Layouts/MainLayout.csx";

var Server = new XServer().SetAppComponent(Layout());
Server.AddPrefixes([
    "http://localhost:65123",
    "js://127.0.0.1:65124"
]);

Server.Start();
```

### Powerful Service Injection

Services are at the application level.

Easily register and inject services to organize your application:

```cs

@Services {
    services.AddSingleton<BottomDrawerService>();
    services.AddSingleton<LeftDrawerService>();
    services.AddSingleton<NavBarService>();
    services.AddSingleton<ViewService>();
}

export dynamic Layout() {
    return  
    <html lang="en">
        <head>
            <title>Vibe App</title>
        </head>
        <body>
            {ViewService}
        </body>
    </html>;
}
```

---

## Example Project

Here’s an example `package.json` for a project using Vibe:

```json
{
  "name": "vibe-app",
  "version": "1.0.0",
  "main": "Main.csx",
  "type": "module",
  "scripts": {
    "start": "dotnet run",
    "build": "dotnet build"
  },
  "dependencies": {
    "niftycsx": "^1.0.0"
  }
}
```

---

## Why Vibe Stands Out

- **Minimal Boilerplate**: Start projects quickly with minimal setup.
- **Single Language**: Build front-end, back-end, and APIs using only C#.
- **Flexible and Modular**: Design applications that scale easily with services and components.
- **Rapid Development**: Focus on building features without repetitive configuration.

---

## License

Vibe is licensed under an MIT License.
---

Experience the simplicity and power of Vibe and start building today!
