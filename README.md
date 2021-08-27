# GherkinSyncTool

GherkinSyncTool is an open-source console application, that synchronizes tests scenarios in
a [Gherkin syntax](https://cucumber.io/docs/gherkin/) (also known as feature files) with a test management system or any
other destination.

## Supported test management systems

- TestRail

## Installation

- Install [.NET](https://dotnet.microsoft.com/download)
- Build solution

```
dotnet build
```

- Run app

```
dotnet .\.\GherkinSyncTool\bin\Debug\net5.0\GherkinSyncTool.dll
```

### TestRail

For the correct work of the tool, the TestRail test template should have custom fields, which are presented in the table
below. The template should not contain the required fields. You can use existing template or create a new one.

| System Name       | Type        |
| ----------------- | ----------- |
| `preconds`        | Text type   |
| `steps_separated` | Step type   |
| `custom_tags`     | String type |

## Configuration

GherkinSyncTool can be configured in three ways. The priority corresponds to the list order.

1. appsettings.json. [Example](GherkinSyncTool\appsettings.json).
2. Environment variables
3. Command-line arguments

### Common settings

| Parameter     | Description                                                                                          | Required |
| ------------- | ---------------------------------------------------------------------------------------------------- | :------: |
| BaseDirectory | Absolute or relative to application folder path that contains *.feature files                        | Yes      |
| TagIdPrefix   | A tag prefix that will be used for mark test scenarios as synchronized with a test management system | No       |

### Formatting settings

| Parameter      | Description            | Required |
| -------------- | ---------------------- | :------: |
| TagIndentation | Left indent for tag ID | No       |

### TestRail settings

| Parameter                  | Description                                                                                   | Required |
| -------------------------- | --------------------------------------------------------------------------------------------- | :------: |
| ProjectId                  | ID of a project that will be used for synchronization                                         | Yes      |
| SuiteId                    | ID of a suite that will be used as a parent for creating sections                             | Yes      |
| TemplateId                 | ID of a template that will be used for creating or updating test cases                        | Yes      |
| RetriesCount               | Count of retries in case of TestRail server returns a not success status code                 | No       |
| PauseBetweenRetriesSeconds | Pause between retries in seconds in case of TestRail server returns a not success status code | No       |
| BaseUrl                    | TestRail URL address                                                                          | Yes      |
| UserName                   | TestRail user name                                                                            | Yes      |
| Password                   | TestRail password                                                                             | Yes      |

## Usage

GherkinSyncTool scans the files in the specified folder for the * .feature files. Then sends API calls to a tests
management system to create or update test cases. Received test ID populated into the feature files as tags for the
following synchronization.

## History

The project selected a test strategy having a single place for storing autotests and manual tests. The test strategy
should reduce the gap between manual and automation tests and make support easier. To achieve this, Gherkin syntax was
chosen. The process of describing test scenarios in such a way is a behavior-driven development (BDD). The project uses
the TestRail test management system. GherkinSyncTool allows having both manual and automated test scenarios in TestRail
so that it is possible to create test runs and have an understanding of what is automated and what should be executed
manually.

## Contribution

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
Please make sure to update tests as appropriate.

## License

Quantori GherkinSyncTool is released under [Apache License, Version 2.0](LICENSE)