1. Route for testing is `localhost:7086/sk-usage/model/gemini-2.5-flash?input=Hello`

2. Implemented controller for the route at `firewall-dotnet\sample-apps\DotNetCore.Sample.App\Controllers\SkUsageController.cs`

3. Pausing execution in `OnLLMCallCompleted` inside `LLMPatcher.cs` shows the model and number of input and output tokens

![Submission](submission.png)
