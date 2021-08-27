testrail-client
===============

.Net Standard 2.0 implementation of the TestRail API.

### Getting Started

```C#
var client = new TestRailClient("https://[your-organization].testrail.io", username, password);
var project = client.GetProjects().First();
var runs = client.GetRuns(project.ID);
var suite = client.GetSuites(project.ID).First();
var cases = client.GetCases(project.ID, suite.ID);
for (var case in cases) {
    Console.WriteLine($"Test Case {case.ID}: {case.Title}");
}
```

### To Modify

Solution was built using Visual Studio 2017 (make sure you have the latest version).

## To Release a New Version
- pull latest
- update the [TestRail.csproj](TestRail/TestRail.csproj) file with the new `Version` and `Release Notes`
- commit and push changes
- pull new code (just for sanity)
- restore nuget dependencies: `.nuget\nuget.exe restore`
- rebuild the project using MS Build with the following command: `msbuild /t:pack /p:Configuration=Release`
  - Make sure that you have msbuild in your path
  - The new .nupkg file will be in the `TestRail\bin\Release\` directory.
- `NuGet push TestRail.<version>.nupkg`

## Unit Testing
- Update the `appsettings.json` with the correct values for the TestRail instance you want to hit.
- Run the unit tests from VS Test Explorer or by running `dotnet test TestRail.Tests\TestRail.Tests.csproj` from the root directory.

## Version History
#### 3.1.2
- Fix error on JObject creation in RequestResult
#### 3.1.1
- Fixed a bug with the client not being able to authenticate.
- Added a testing project.
#### 3.1.0
- Updated the `AddCase()` method to include the template id in the request.
- Created new methods `AddResults()` and `AddResultsForCases()` to submit results in bulk.
- Updated the `GetResults()`, `GetResultsForCase()`, and `GetResultsForRun()` methods to allow filtering by status id.

**NOTE:** Custom fields are not yet supported when making bulk submissions.
#### 3.0.1
- Fixed an issue in the `AddRun()` method that made it impossible to add a new run if there is no milestone in project.
#### 3.0
- A successful command will now return the appropriate TestRail object according to the official documentation.
  - For example: `AddResult()` will now return the newly created `Result` objects.
  - Official documentation can be found here: http://docs.gurock.com/testrail-api2/start
- An unsuccessful command will now include the exception/error that was thrown to better understand what went wrong.
#### 2.0.3
- Add the ability to delete a test run.
#### 2.0.2
- Add Milestones property to handle sub-milestones in GetMilestone call.
#### 2.0.1
- Add description to Section and add custom to run, plan, and other APIs.
- Fix dotnet core bug with HttpWebRequest.
#### 2.0
- Converted the application to .Net Standard 2.0
  - The client should work with any project on: .Net Framework 4.5, 4.6.1, .Net Core &gt;= 2.0, and .Net Standard &gt;= 2.0.
#### 1.0.0.15
- Bug Fix
  - Timespan field for Result class
- Add isStarted property to Milestone class
- Update Newtonsoft.Json NuGet package
#### 1.0.0.12
- Bug Fix
  - Run's JSON Parse method threw possible ArgumentNullException on created_on field
#### 1.0.0.11
- Update for VS 2015 and C# 6
- Update Nuget packages
- Features
  - Add CreatedOn property to Run class
#### 1.0.0.10
- TestRail types contain the Raw JSON object received
#### 1.0.0.9
- Add more fields into Plan
- Update Nuget libraries
#### 1.0.0.8
- Fix Nuget spec file
#### 1.0.0.7
- Update dependencies for this package
#### 1.0.0.6
- Bug
  - Result Status Enum for Untested had wrong ID
- Feature
  - Get Plan now populates complete run objects
  - Get for Configs
  - Ability to create a test plan with configs
  - Config Ids added to plan entry
#### 1.0.0.5
- Update Json.net Nuget Package
