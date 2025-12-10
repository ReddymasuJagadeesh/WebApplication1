# Complete Project Explanation - WebApplication1

## 📚 Table of Contents
1. [How the Application Starts](#how-the-application-starts)
2. [Project Structure - Folder by Folder](#project-structure---folder-by-folder)
3. [Request-Response Cycle](#request-response-cycle)
4. [MVC Pattern in Action](#mvc-pattern-in-action)
5. [Database Connection Flow](#database-connection-flow)
6. [Complete Code Walkthrough](#complete-code-walkthrough)
7. [How Everything Connects Together](#how-everything-connects-together)

---

## 🚀 How the Application Starts

### Step 1: Application Entry Point - `Program.cs`

When you run the application (by pressing F5 or running `dotnet run`), the operating system looks for the entry point. In ASP.NET Core, this is the `Program.cs` file.

```csharp
// Program.cs - This is where everything begins!
```

**What happens here:**

1. **`var builder = WebApplication.CreateBuilder(args);`**
   - Creates a "builder" object that will construct your web application
   - `args` are command-line arguments (if any)
   - This builder reads configuration from `appsettings.json`, environment variables, etc.

2. **`builder.Services.AddControllersWithViews();`**
   - **What it does:** Registers the MVC (Model-View-Controller) framework
   - **Why:** Tells ASP.NET Core that you're using MVC pattern
   - **What gets registered:**
     - Controllers (like `StudentsController`, `HomeController`)
     - Views (Razor pages like `Index.cshtml`)
     - Model binding (automatically converts form data to C# objects)
     - View engines (Razor engine that converts `.cshtml` to HTML)

3. **`builder.Services.AddDbContext<ApplicationDbContext>(...);`**
   - **What it does:** Registers your database context for dependency injection
   - **How it works:**
     - When any controller needs `ApplicationDbContext`, ASP.NET Core automatically creates it
     - The connection string comes from `appsettings.json`
     - Uses PostgreSQL via `UseNpgsql()`
   - **Why dependency injection:** Instead of creating database connections manually everywhere, you just ask for it in the constructor, and ASP.NET Core provides it

4. **`var app = builder.Build();`**
   - Takes all the services you registered and builds the actual web application
   - This creates the HTTP pipeline (the path requests will follow)

5. **HTTP Pipeline Configuration:**
   ```csharp
   if (!app.Environment.IsDevelopment())
   {
       app.UseExceptionHandler("/Home/Error");  // In production, show error page
       app.UseHsts();  // Security: Force HTTPS
   }
   ```
   - **Development vs Production:** Different error handling based on environment

6. **`app.UseHttpsRedirection();`**
   - Automatically redirects HTTP requests to HTTPS (secure connection)

7. **`app.UseRouting();`**
   - Enables routing - matches URLs to controllers and actions
   - Example: `/Students/Index` → `StudentsController.Index()`

8. **`app.MapControllerRoute(...);`**
   - **This is the routing rule:**
     ```
     Pattern: "{controller=Home}/{action=Index}/{id?}"
     ```
   - **How it works:**
     - `/` → `HomeController.Index()` (defaults)
     - `/Students` → `StudentsController.Index()` (action defaults to Index)
     - `/Students/Create` → `StudentsController.Create()`
     - `/Students/Edit/5` → `StudentsController.Edit(5)`
   - The `?` means `id` is optional

9. **`app.Run();`**
   - Starts the web server
   - Listens for HTTP requests on the port specified in `launchSettings.json`
   - **Default ports:** HTTP: 5287, HTTPS: 7034

---

## 📁 Project Structure - Folder by Folder

### 1. **Controllers/** - The Traffic Directors

**Purpose:** Controllers are like traffic directors. They receive HTTP requests, decide what to do, and send responses.

**How it works:**
- When a user visits a URL, the routing system finds the matching controller
- The controller method (action) executes
- It can:
  - Read from database
  - Perform business logic
  - Return a view (HTML page)
  - Return JSON data
  - Redirect to another page

#### `HomeController.cs` - Simple Example

```csharp
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();  // Returns Views/Home/Index.cshtml
    }
}
```

**What happens:**
1. User visits `http://localhost:5287/` or `http://localhost:5287/Home`
2. Routing matches to `HomeController.Index()`
3. Method executes and calls `View()`
4. ASP.NET Core looks for `Views/Home/Index.cshtml`
5. Razor engine processes the view and generates HTML
6. HTML is sent back to the browser

#### `StudentsController.cs` - Complex Example

This controller handles all student-related operations. Let's break down each method:

**Constructor:**
```csharp
private readonly ApplicationDbContext context;

public StudentsController(ApplicationDbContext context)
{
    this.context = context;  // Dependency injection provides this!
}
```
- **Dependency Injection Magic:** When ASP.NET Core creates `StudentsController`, it sees it needs `ApplicationDbContext`
- It automatically creates the database context and passes it to the constructor
- You don't need to write `new ApplicationDbContext()` - it's automatic!

**Index Method (List with Pagination):**
```csharp
public async Task<IActionResult> Index(int page = 1, int pageSize = 3)
```
- **`async Task<IActionResult>`:** This method is asynchronous (doesn't block the server)
- **Parameters from URL:** `?page=2&pageSize=5` becomes `page=2, pageSize=5`
- **What it does:**
  1. Counts total students: `await context.Students.CountAsync()`
  2. Calculates total pages
  3. Skips and takes records for pagination: `.Skip((page-1)*pageSize).Take(pageSize)`
  4. Creates a ViewModel with the data
  5. Returns the view

**Create Method (GET):**
```csharp
[HttpGet]
public IActionResult Create()
{
    return View();  // Shows empty form
}
```
- **`[HttpGet]`:** Only responds to GET requests (when user visits the page)
- Returns the form view

**Create Method (POST):**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Students student)
```
- **`[HttpPost]`:** Only responds to POST requests (when form is submitted)
- **`[ValidateAntiForgeryToken]`:** Security - prevents CSRF attacks
- **`Students student`:** Model binding automatically converts form fields to a `Students` object
- **`ModelState.IsValid`:** Checks if validation rules (from `students.cs` model) passed
- If valid: saves to database and redirects
- If invalid: shows the form again with error messages

**Edit Method - The Complex One:**
The Edit method is interesting because it handles ID changes:

```csharp
if (student.Id != originalId)  // User changed the ID
{
    // Use transaction to ensure data integrity
    using var tx = await context.Database.BeginTransactionAsync();
    
    // Create new record with new ID
    context.Students.Add(newStudent);
    
    // Delete old record
    context.Students.Remove(existing);
    
    // Save everything or nothing (transaction)
    await context.SaveChangesAsync();
    await tx.CommitAsync();
}
```

**Why transactions?** If something fails halfway, the database rolls back - no partial updates!

---

### 2. **Models/** - Data Structures and Rules

**Purpose:** Models define:
- What data looks like (structure)
- What rules data must follow (validation)

#### `students.cs` - The Entity Model

```csharp
public class Students
{
    [Range(1, int.MaxValue, ErrorMessage = "...")]
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Name is required.")]
    [RegularExpression(@"^[A-Za-z\s]+$", ...)]
    public string Name { get; set; } = string.Empty;
}
```

**How validation works:**
1. **Data Annotations:** The `[Required]`, `[RegularExpression]` attributes are "data annotations"
2. **Server-side:** When form is submitted, `ModelState.IsValid` checks these rules
3. **Client-side:** jQuery Validation reads these attributes and validates in the browser before submission

**Attribute Breakdown:**
- `[Required]`: Field cannot be empty
- `[RegularExpression]`: Must match the pattern (regex)
- `[Range]`: Number must be within range
- `ErrorMessage`: What to show if validation fails

#### `StudentsIndexViewModel.cs` - View Model

**Why View Models?** Sometimes a view needs more than just the entity data.

```csharp
public class StudentsIndexViewModel
{
    public IEnumerable<Students> Students { get; set; }  // The actual data
    public int Page { get; set; }  // Current page number
    public int PageSize { get; set; }  // Items per page
    public int TotalItems { get; set; }  // Total count
    
    // Computed properties (calculated, not stored)
    public int TotalPages => PageSize == 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
```

**Difference:**
- **Entity Model (`Students`):** Represents database table structure
- **View Model (`StudentsIndexViewModel`):** Represents what the view needs (data + pagination info)

---

### 3. **Views/** - The User Interface

**Purpose:** Views are HTML templates that generate the final HTML sent to browsers.

**How Razor Works:**
- Razor is a templating engine
- Mixes HTML with C# code
- `@` symbol starts C# code
- Files end with `.cshtml` (C# + HTML)

#### View Hierarchy:

**`_ViewStart.cshtml`:**
```razor
@{
    Layout = "_Layout";
}
```
- **Runs first** for every view
- Sets the default layout (master template)
- Like a "wrapper" that goes around all pages

**`_ViewImports.cshtml`:**
```razor
@using WebApplication1
@using WebApplication1.Models
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```
- **Imports namespaces** so you don't need `@using` in every view
- **Tag Helpers:** Enable special HTML-like syntax:
  - `<a asp-action="Index">` instead of `<a href="/Students/Index">`
  - ASP.NET Core generates the correct URL automatically

**`_Layout.cshtml` - The Master Template:**
```html
<!DOCTYPE html>
<html>
<head>
    <!-- CSS files -->
</head>
<body>
    <header>
        <!-- Navigation bar -->
    </header>
    <div class="container">
        @RenderBody()  <!-- Your view content goes here -->
    </div>
    <footer>
        <!-- Footer -->
    </footer>
    <!-- JavaScript files -->
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

**How it works:**
1. User requests a page
2. `_ViewStart.cshtml` sets layout to `_Layout.cshtml`
3. Your view (e.g., `Index.cshtml`) is processed
4. `@RenderBody()` in `_Layout.cshtml` is replaced with your view's content
5. Final HTML is sent to browser

#### Example: `Views/Students/Index.cshtml`

```razor
@model WebApplication1.Models.StudentsIndexViewModel
```
- **`@model`:** Tells Razor what type of data this view expects
- Strongly typed - you get IntelliSense in Visual Studio

```razor
@foreach (var s in students)
{
    <tr>
        <td>@s.Id</td>
        <td>@s.Name</td>
    </tr>
}
```
- **`@foreach`:** C# loop in Razor syntax
- **`@s.Id`:** Outputs the value (escaped for security)

```razor
<a asp-action="Details" asp-route-id="@s.Id" class="btn btn-info">Details</a>
```
- **Tag Helper syntax:** `asp-action` and `asp-route-id`
- ASP.NET Core generates: `<a href="/Students/Details/5">`
- **Why use this?** If you change routing, URLs update automatically!

**Pagination Logic:**
```razor
@for (var p = 1; p <= totalPages; p++)
{
    <li class="page-item @(p == page ? "active" : "")">
        <a class="page-link" asp-action="Index" asp-route-page="@p">@p</a>
    </li>
}
```
- Generates page number links
- Current page gets "active" class (highlighted)

---

### 4. **Data/** - Database Connection

**Purpose:** The `ApplicationDbContext` is your bridge to the database.

#### `ApplicationDbContext.cs`

```csharp
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<Students> Students { get; set; }
}
```

**How it works:**
1. **Inherits from `DbContext`:** This is Entity Framework Core's base class
2. **`DbSet<Students>`:** Represents the `Students` table in the database
3. **Dependency Injection:** The constructor receives `DbContextOptions` which contains:
   - Connection string
   - Database provider (PostgreSQL)
   - Other settings

**Usage in Controller:**
```csharp
var students = await context.Students.ToListAsync();
```
- **`context.Students`:** Access the Students table
- **`.ToListAsync()`:** Executes SQL query and returns results
- **Generated SQL:** `SELECT * FROM "Students"`

**Entity Framework Magic:**
- You write C# code: `context.Students.Where(s => s.Id > 5)`
- EF Core translates to SQL: `SELECT * FROM "Students" WHERE "Id" > 5`
- You don't write SQL manually!

---

### 5. **Migrations/** - Database Schema Version Control

**Purpose:** Migrations are like "version control for your database schema."

#### How Migrations Work:

1. **You define models** in C# (`Students` class)
2. **You create a migration:** `dotnet ef migrations add Init`
3. **EF Core generates C# code** that creates/updates database tables
4. **You apply migration:** `dotnet ef database update`
5. **Database is created/updated** to match your models

#### `20251204080617_Init.cs`

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Students",
        columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", 
                    NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            Name = table.Column<string>(type: "text", nullable: false),
            Email = table.Column<string>(type: "text", nullable: false),
            Mobileno = table.Column<string>(type: "text", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Students", x => x.Id);
        });
}
```

**What this does:**
- **`Up()` method:** Applies the migration (creates the table)
- **`Down()` method:** Rolls back the migration (drops the table)
- Creates the `Students` table with columns matching your `Students` model

**Why migrations?**
- Track database changes over time
- Can roll back if something goes wrong
- Team members can apply same changes to their databases
- Version control for database schema

---

### 6. **wwwroot/** - Static Files

**Purpose:** Contains files sent directly to the browser without processing.

**Structure:**
```
wwwroot/
├── css/          # Stylesheets
├── js/           # JavaScript files
└── lib/          # Third-party libraries (Bootstrap, jQuery)
```

**How it works:**
- Files in `wwwroot` are publicly accessible
- `~/css/site.css` → `http://localhost:5287/css/site.css`
- `~` means "root of wwwroot"

#### `wwwroot/js/email-validation.js`

```javascript
document.addEventListener("DOMContentLoaded", function () {
    const emailInput = document.querySelector("#Email");
    
    emailInput.addEventListener("input", function () {
        // Real-time validation as user types
    });
});
```

**What this does:**
- **`DOMContentLoaded`:** Waits for HTML to load
- **`querySelector("#Email")`:** Finds the email input field
- **`addEventListener("input", ...)`:** Listens for typing
- Provides instant feedback without submitting the form

**Client-side vs Server-side Validation:**
- **Client-side (JavaScript):** Fast, immediate feedback, but can be bypassed
- **Server-side (C#):** Always runs, secure, but requires form submission
- **Best practice:** Use both! Client-side for UX, server-side for security

---

### 7. **Properties/** - Configuration

#### `launchSettings.json`

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5287"
    },
    "https": {
      "applicationUrl": "https://localhost:7034;http://localhost:5287"
    }
  }
}
```

**What it does:**
- Defines how the application runs
- Sets ports for HTTP and HTTPS
- Sets environment (Development, Production, etc.)
- Only used during development (not deployed)

---

### 8. **appsettings.json** - Application Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;port=5432;Database=StudentsDemo;..."
  }
}
```

**How it's used:**
- `Program.cs` reads this: `builder.Configuration.GetConnectionString("DefaultConnection")`
- Contains:
  - Database connection strings
  - Logging settings
  - Other configuration

**Security Note:** Never commit passwords to source control! Use environment variables or secrets manager in production.

---

## 🔄 Request-Response Cycle

Let's trace a complete request from browser to database and back:

### Example: User clicks "Add New Student" button

**Step 1: Browser sends HTTP request**
```
GET http://localhost:5287/Students/Create
```

**Step 2: Routing matches the URL**
- Pattern: `{controller}/{action}/{id?}`
- Matches: `controller=Students`, `action=Create`
- Finds: `StudentsController.Create()` method

**Step 3: Controller method executes**
```csharp
[HttpGet]
public IActionResult Create()
{
    return View();  // Returns Views/Students/Create.cshtml
}
```

**Step 4: View is processed**
1. `_ViewStart.cshtml` sets layout
2. `_Layout.cshtml` wraps the page
3. `Create.cshtml` is rendered
4. Razor processes `@model`, `@Html.AntiForgeryToken()`, etc.
5. HTML is generated

**Step 5: HTML sent to browser**
```html
<!DOCTYPE html>
<html>
<head>...</head>
<body>
    <form action="/Students/Create" method="post">
        <input name="Id" />
        <input name="Name" />
        ...
    </form>
</body>
</html>
```

**Step 6: User fills form and submits**

**Step 7: Browser sends POST request**
```
POST http://localhost:5287/Students/Create
Content-Type: application/x-www-form-urlencoded

Id=1&Name=John&Email=john@gmail.com&Mobileno=1234567890
```

**Step 8: Model Binding**
- ASP.NET Core automatically creates a `Students` object
- Maps form fields to properties:
  - `Id=1` → `student.Id = 1`
  - `Name=John` → `student.Name = "John"`

**Step 9: Validation**
```csharp
if (ModelState.IsValid)  // Checks all [Required], [RegularExpression], etc.
{
    // Save to database
}
```

**Step 10: Database operation**
```csharp
context.Students.Add(student);
await context.SaveChangesAsync();
```
- EF Core generates SQL: `INSERT INTO "Students" ("Id", "Name", ...) VALUES (1, 'John', ...)`
- Executes against PostgreSQL
- Returns success/failure

**Step 11: Redirect**
```csharp
return RedirectToAction(nameof(Index));
```
- Sends HTTP 302 redirect
- Browser automatically requests `/Students/Index`

**Step 12: Index page loads**
- Same cycle repeats for `Index` action
- Queries database
- Renders list of students
- User sees the new student in the list!

---

## 🎯 MVC Pattern in Action

**MVC = Model-View-Controller**

### Model (Data + Rules)
- **`Students` class:** Defines data structure and validation rules
- **`ApplicationDbContext`:** Handles database operations
- **Location:** `Models/` and `Data/` folders

### View (User Interface)
- **Razor files (.cshtml):** HTML templates
- **Location:** `Views/` folder
- **Purpose:** Display data, collect user input

### Controller (Logic + Coordination)
- **Controller classes:** Handle requests, process data, return views
- **Location:** `Controllers/` folder
- **Purpose:** Connect Models and Views

### Flow:
```
User Request
    ↓
Controller (receives request)
    ↓
Model (reads/writes data)
    ↓
Controller (processes data)
    ↓
View (renders HTML)
    ↓
Response to User
```

**Benefits:**
- **Separation of concerns:** Each part has one job
- **Testability:** Can test controllers without views
- **Maintainability:** Easy to find and fix issues
- **Reusability:** Models can be used in multiple views

---

## 🗄️ Database Connection Flow

### How Entity Framework Core Works:

1. **You define models** (C# classes)
   ```csharp
   public class Students { public int Id { get; set; } }
   ```

2. **You create DbContext**
   ```csharp
   public DbSet<Students> Students { get; set; }
   ```

3. **You write LINQ queries**
   ```csharp
   var students = await context.Students.Where(s => s.Id > 5).ToListAsync();
   ```

4. **EF Core translates to SQL**
   ```sql
   SELECT * FROM "Students" WHERE "Id" > 5
   ```

5. **PostgreSQL executes SQL**

6. **EF Core converts results back to C# objects**

### Connection String Breakdown:

```
Server=localhost;          # Database server address
port=5432;                 # PostgreSQL default port
Database=StudentsDemo;     # Database name
UserId=postgres;           # Username
Password=jaga@123R         # Password
```

### Dependency Injection Flow:

1. **Registration (Program.cs):**
   ```csharp
   builder.Services.AddDbContext<ApplicationDbContext>(...);
   ```

2. **Request comes in**

3. **Controller needs ApplicationDbContext:**
   ```csharp
   public StudentsController(ApplicationDbContext context)
   ```

4. **ASP.NET Core creates it automatically:**
   - Reads connection string
   - Creates database connection
   - Passes to constructor

5. **Controller uses it:**
   ```csharp
   var students = await context.Students.ToListAsync();
   ```

6. **After request completes:**
   - Connection is disposed (cleaned up)
   - Ready for next request

---

## 🔗 How Everything Connects Together

### Complete Flow Diagram:

```
┌─────────────┐
│   Browser   │
│  (User)     │
└──────┬──────┘
       │ HTTP Request: GET /Students/Index
       ↓
┌─────────────────────────────────────┐
│         ASP.NET Core                 │
│  ┌──────────────────────────────┐   │
│  │      Routing System           │   │
│  │  Matches URL to Controller    │   │
│  └──────────┬────────────────────┘   │
│             ↓                         │
│  ┌──────────────────────────────┐   │
│  │   StudentsController         │   │
│  │   Index() method             │   │
│  └──────────┬────────────────────┘   │
│             ↓                         │
│  ┌──────────────────────────────┐   │
│  │  ApplicationDbContext        │   │
│  │  (Dependency Injection)      │   │
│  └──────────┬────────────────────┘   │
│             ↓                         │
│  ┌──────────────────────────────┐   │
│  │   Entity Framework Core      │   │
│  │   Generates SQL Query        │   │
│  └──────────┬────────────────────┘   │
└─────────────┼─────────────────────────┘
              ↓ SQL: SELECT * FROM Students
┌─────────────┐
│ PostgreSQL  │
│  Database   │
└──────┬──────┘
       │ Returns data rows
       ↓
┌─────────────────────────────────────┐
│  EF Core converts rows to Students   │
│  objects                             │
└──────────┬───────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│  Controller creates ViewModel        │
│  Returns View(ViewModel)             │
└──────────┬───────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│  Razor Engine processes View         │
│  Views/Students/Index.cshtml         │
│  + _Layout.cshtml                    │
└──────────┬───────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│  HTML generated and sent to browser  │
└─────────────────────────────────────┘
```

---

## 🎓 Key Concepts Explained

### 1. **Dependency Injection (DI)**

**What it is:** Instead of creating objects yourself, you "ask" for them, and the framework provides them.

**Without DI (Bad):**
```csharp
public StudentsController()
{
    var context = new ApplicationDbContext(...);  // Manual creation
}
```

**With DI (Good):**
```csharp
public StudentsController(ApplicationDbContext context)  // Framework provides it
{
    this.context = context;
}
```

**Benefits:**
- Easier testing (can inject fake objects)
- Automatic cleanup
- Centralized configuration

### 2. **Async/Await**

**What it is:** Allows the server to handle other requests while waiting for database operations.

**Synchronous (Blocks):**
```csharp
public IActionResult Index()
{
    var students = context.Students.ToList();  // Server waits, can't handle other requests
    return View(students);
}
```

**Asynchronous (Non-blocking):**
```csharp
public async Task<IActionResult> Index()
{
    var students = await context.Students.ToListAsync();  // Server can handle other requests
    return View(students);
}
```

**Why it matters:** Your server can handle 1000 requests concurrently instead of one at a time!

### 3. **Model Binding**

**What it is:** Automatically converts HTTP request data (form fields, query strings) to C# objects.

**Example:**
```html
<form method="post">
    <input name="Id" value="1" />
    <input name="Name" value="John" />
</form>
```

**Automatically becomes:**
```csharp
public async Task<IActionResult> Create(Students student)
{
    // student.Id = 1
    // student.Name = "John"
    // No manual parsing needed!
}
```

### 4. **Tag Helpers**

**What they are:** Special HTML attributes that ASP.NET Core processes.

**Instead of:**
```html
<a href="/Students/Edit/5">Edit</a>
```

**You write:**
```html
<a asp-action="Edit" asp-route-id="5">Edit</a>
```

**Benefits:**
- URLs update automatically if routing changes
- Compile-time checking
- IntelliSense support

### 5. **Anti-Forgery Tokens**

**What they are:** Security tokens that prevent CSRF (Cross-Site Request Forgery) attacks.

**How it works:**
1. Form includes hidden token: `@Html.AntiForgeryToken()`
2. Server validates token on POST
3. If token is missing/invalid, request is rejected

**Why it matters:** Prevents malicious websites from submitting forms on your behalf.

---

## 📝 Summary

This project is a **complete MVC web application** that:

1. **Starts** with `Program.cs` configuring services and HTTP pipeline
2. **Routes** requests to appropriate controllers
3. **Controllers** handle business logic and coordinate with database
4. **Models** define data structure and validation rules
5. **Views** render HTML using Razor templating
6. **Database** is accessed through Entity Framework Core
7. **Static files** (CSS, JS) are served from `wwwroot`

**The flow is:**
```
Request → Routing → Controller → Database → Model → View → Response
```

**Key technologies:**
- **ASP.NET Core MVC:** Web framework
- **Entity Framework Core:** Database access
- **PostgreSQL:** Database
- **Razor:** View engine
- **Bootstrap:** UI framework
- **jQuery:** JavaScript library

This architecture is **scalable**, **maintainable**, and follows **industry best practices**!

