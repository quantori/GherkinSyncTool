# Quantori GherkinSyncTool

Copyright (c) 2022 Quantori.

Quantori Allure TestOps client is an open-source .Net SDK for the Allure TestOps server https://qameta.io/.

## How it works

1. Add nuget package
2. Initialize client:

```cs
var url = "https://example.testops.cloud/";
var token = "2dedb182-4870-17c2-bfe9-95c01a221001";
var projectId = 1;

var allureClient = AllureClient.Get(url, token);

var testCases = allureClient.GetTestCasesAsync(projectId).Result;
```

## License

Quantori GherkinSyncTool is released under [Apache License, Version 2.0](LICENSE)
