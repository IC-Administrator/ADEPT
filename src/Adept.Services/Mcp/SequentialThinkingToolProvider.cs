using Adept.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Adept.Services.Mcp
{
    /// <summary>
    /// Provider for sequential thinking tools
    /// </summary>
    public class SequentialThinkingToolProvider : IMcpToolProvider
    {
        private readonly ILogger<SequentialThinkingToolProvider> _logger;
        private readonly List<IMcpTool> _tools = new List<IMcpTool>();
        private readonly ILlmService _llmService;

        /// <summary>
        /// Gets the name of the provider
        /// </summary>
        public string ProviderName => "SequentialThinking";

        /// <summary>
        /// Gets the available tools
        /// </summary>
        public IEnumerable<IMcpTool> Tools => _tools;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequentialThinkingToolProvider"/> class
        /// </summary>
        /// <param name="llmService">The LLM service</param>
        /// <param name="logger">The logger</param>
        public SequentialThinkingToolProvider(ILlmService llmService, ILogger<SequentialThinkingToolProvider> logger)
        {
            _llmService = llmService;
            _logger = logger;

            // Initialize tools
            _tools.Add(new StepByStepReasoningTool(_llmService, _logger));
            _tools.Add(new ProblemDecompositionTool(_llmService, _logger));
            _tools.Add(new SelfCritiqueTool(_llmService, _logger));
        }

        /// <summary>
        /// Initializes the provider
        /// </summary>
        public Task InitializeAsync()
        {
            _logger.LogInformation("Sequential Thinking tool provider initialized");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets a tool by name
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <returns>The tool or null if not found</returns>
        public IMcpTool? GetTool(string toolName)
        {
            return _tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Executes a tool
        /// </summary>
        /// <param name="toolName">The name of the tool</param>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
        {
            var tool = GetTool(toolName);
            if (tool == null)
            {
                return McpToolResult.Error($"Tool {toolName} not found");
            }

            try
            {
                return await tool.ExecuteAsync(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
                return McpToolResult.Error($"Error executing tool: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the tool schema for all available tools
        /// </summary>
        /// <returns>The tool schema</returns>
        public IEnumerable<McpToolSchema> GetToolSchema()
        {
            return _tools.Select(t => t.Schema);
        }
    }

    /// <summary>
    /// Tool for step-by-step reasoning
    /// </summary>
    public class StepByStepReasoningTool : IMcpTool
    {
        private readonly ILlmService _llmService;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "step_by_step_reasoning";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Performs step-by-step reasoning to solve a problem";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["problem"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The problem to solve",
                    Required = true
                },
                ["num_steps"] = new McpParameterSchema
                {
                    Type = "integer",
                    Description = "The number of steps to use",
                    Required = false,
                    DefaultValue = 5
                },
                ["model"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The LLM model to use",
                    Required = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Step-by-step reasoning and solution"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="StepByStepReasoningTool"/> class
        /// </summary>
        /// <param name="llmService">The LLM service</param>
        /// <param name="logger">The logger</param>
        public StepByStepReasoningTool(ILlmService llmService, ILogger logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("problem", out var problemObj) || problemObj == null)
                {
                    return McpToolResult.Error("Problem parameter is required");
                }

                var problem = problemObj.ToString() ?? "";
                var numSteps = parameters.TryGetValue("num_steps", out var numStepsObj) && numStepsObj != null
                    ? Convert.ToInt32(numStepsObj)
                    : 5;
                var model = parameters.TryGetValue("model", out var modelObj) && modelObj != null
                    ? modelObj.ToString()
                    : null;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(problem))
                {
                    return McpToolResult.Error("Problem cannot be empty");
                }

                if (numSteps < 1 || numSteps > 10)
                {
                    return McpToolResult.Error("Number of steps must be between 1 and 10");
                }

                // Create the prompt
                var prompt = $@"I need you to solve the following problem using step-by-step reasoning.
Break down your thinking into exactly {numSteps} clear steps, and then provide a final answer.

Problem: {problem}

Format your response as follows:
Step 1: [Your reasoning for step 1]
Step 2: [Your reasoning for step 2]
...
Step {numSteps}: [Your reasoning for step {numSteps}]

Final Answer: [Your final answer]

Make sure to be thorough and clear in your reasoning.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);
                var responseText = response.Message.Content;

                // Parse the response
                var steps = new List<string>();
                var finalAnswer = "";

                var lines = responseText.Split('\n');
                var currentStep = new StringBuilder();
                var inStep = false;
                var stepNumber = 1;

                foreach (var line in lines)
                {
                    if (line.StartsWith($"Step {stepNumber}:"))
                    {
                        if (inStep)
                        {
                            steps.Add(currentStep.ToString().Trim());
                            currentStep.Clear();
                        }
                        inStep = true;
                        currentStep.AppendLine(line);
                        stepNumber++;
                    }
                    else if (line.StartsWith("Final Answer:"))
                    {
                        if (inStep)
                        {
                            steps.Add(currentStep.ToString().Trim());
                            currentStep.Clear();
                            inStep = false;
                        }
                        finalAnswer = line.Substring("Final Answer:".Length).Trim();
                    }
                    else if (inStep)
                    {
                        currentStep.AppendLine(line);
                    }
                }

                // Add the last step if we were in one
                if (inStep && currentStep.Length > 0)
                {
                    steps.Add(currentStep.ToString().Trim());
                }

                var result = new
                {
                    problem,
                    steps = steps.ToArray(),
                    final_answer = finalAnswer,
                    full_response = responseText
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing step-by-step reasoning");
                return McpToolResult.Error($"Error executing step-by-step reasoning: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for problem decomposition
    /// </summary>
    public class ProblemDecompositionTool : IMcpTool
    {
        private readonly ILlmService _llmService;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "problem_decomposition";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Decomposes a complex problem into smaller, manageable sub-problems";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["problem"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The complex problem to decompose",
                    Required = true
                },
                ["model"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The LLM model to use",
                    Required = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Decomposed sub-problems and approach"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ProblemDecompositionTool"/> class
        /// </summary>
        /// <param name="llmService">The LLM service</param>
        /// <param name="logger">The logger</param>
        public ProblemDecompositionTool(ILlmService llmService, ILogger logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("problem", out var problemObj) || problemObj == null)
                {
                    return McpToolResult.Error("Problem parameter is required");
                }

                var problem = problemObj.ToString() ?? "";
                var model = parameters.TryGetValue("model", out var modelObj) && modelObj != null
                    ? modelObj.ToString()
                    : null;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(problem))
                {
                    return McpToolResult.Error("Problem cannot be empty");
                }

                // Create the prompt
                var prompt = $@"I need you to decompose the following complex problem into smaller, more manageable sub-problems.
For each sub-problem, provide a brief description and approach for solving it.

Complex Problem: {problem}

Format your response as follows:
Problem Analysis: [Analyze the overall problem]

Sub-Problem 1: [Name of sub-problem 1]
Description: [Description of sub-problem 1]
Approach: [Approach to solve sub-problem 1]

Sub-Problem 2: [Name of sub-problem 2]
Description: [Description of sub-problem 2]
Approach: [Approach to solve sub-problem 2]

...

Integration Strategy: [How to integrate the solutions to the sub-problems]

Make sure your decomposition is comprehensive and addresses all aspects of the original problem.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);
                var responseText = response.Message.Content;

                // Parse the response
                var subProblems = new List<Dictionary<string, string>>();
                var problemAnalysis = "";
                var integrationStrategy = "";

                var lines = responseText.Split('\n');
                var currentSection = "";
                var currentSubProblem = new Dictionary<string, string>();
                var inSubProblem = false;

                foreach (var line in lines)
                {
                    if (line.StartsWith("Problem Analysis:"))
                    {
                        currentSection = "analysis";
                        problemAnalysis = line.Substring("Problem Analysis:".Length).Trim();
                    }
                    else if (line.StartsWith("Sub-Problem "))
                    {
                        if (inSubProblem)
                        {
                            subProblems.Add(new Dictionary<string, string>(currentSubProblem));
                            currentSubProblem.Clear();
                        }
                        inSubProblem = true;
                        currentSection = "subproblem";
                        currentSubProblem["name"] = line.Substring(line.IndexOf(':') + 1).Trim();
                    }
                    else if (inSubProblem && line.StartsWith("Description:"))
                    {
                        currentSubProblem["description"] = line.Substring("Description:".Length).Trim();
                    }
                    else if (inSubProblem && line.StartsWith("Approach:"))
                    {
                        currentSubProblem["approach"] = line.Substring("Approach:".Length).Trim();
                    }
                    else if (line.StartsWith("Integration Strategy:"))
                    {
                        if (inSubProblem)
                        {
                            subProblems.Add(new Dictionary<string, string>(currentSubProblem));
                            currentSubProblem.Clear();
                            inSubProblem = false;
                        }
                        currentSection = "integration";
                        integrationStrategy = line.Substring("Integration Strategy:".Length).Trim();
                    }
                    else if (currentSection == "analysis")
                    {
                        problemAnalysis += " " + line.Trim();
                    }
                    else if (currentSection == "subproblem" && inSubProblem)
                    {
                        if (currentSubProblem.ContainsKey("approach"))
                        {
                            currentSubProblem["approach"] += " " + line.Trim();
                        }
                        else if (currentSubProblem.ContainsKey("description"))
                        {
                            currentSubProblem["description"] += " " + line.Trim();
                        }
                    }
                    else if (currentSection == "integration")
                    {
                        integrationStrategy += " " + line.Trim();
                    }
                }

                // Add the last sub-problem if we were in one
                if (inSubProblem && currentSubProblem.Count > 0)
                {
                    subProblems.Add(new Dictionary<string, string>(currentSubProblem));
                }

                var result = new
                {
                    problem,
                    problem_analysis = problemAnalysis,
                    sub_problems = subProblems.ToArray(),
                    integration_strategy = integrationStrategy,
                    full_response = responseText
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing problem decomposition");
                return McpToolResult.Error($"Error executing problem decomposition: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Tool for self-critique
    /// </summary>
    public class SelfCritiqueTool : IMcpTool
    {
        private readonly ILlmService _llmService;
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the name of the tool
        /// </summary>
        public string Name => "self_critique";

        /// <summary>
        /// Gets the description of the tool
        /// </summary>
        public string Description => "Critiques a solution or approach to identify potential issues and improvements";

        /// <summary>
        /// Gets the schema for the tool
        /// </summary>
        public McpToolSchema Schema => new McpToolSchema
        {
            Name = Name,
            Description = Description,
            Parameters = new Dictionary<string, McpParameterSchema>
            {
                ["solution"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The solution or approach to critique",
                    Required = true
                },
                ["context"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "Additional context or requirements",
                    Required = false
                },
                ["model"] = new McpParameterSchema
                {
                    Type = "string",
                    Description = "The LLM model to use",
                    Required = false
                }
            },
            ReturnType = new McpParameterSchema
            {
                Type = "object",
                Description = "Critique of the solution with issues and improvements"
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SelfCritiqueTool"/> class
        /// </summary>
        /// <param name="llmService">The LLM service</param>
        /// <param name="logger">The logger</param>
        public SelfCritiqueTool(ILlmService llmService, ILogger logger)
        {
            _llmService = llmService;
            _logger = logger;
        }

        /// <summary>
        /// Executes the tool
        /// </summary>
        /// <param name="parameters">The parameters for the tool</param>
        /// <returns>The tool result</returns>
        public async Task<McpToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                // Get parameters
                if (!parameters.TryGetValue("solution", out var solutionObj) || solutionObj == null)
                {
                    return McpToolResult.Error("Solution parameter is required");
                }

                var solution = solutionObj.ToString() ?? "";
                var context = parameters.TryGetValue("context", out var contextObj) && contextObj != null
                    ? contextObj.ToString()
                    : "";
                var model = parameters.TryGetValue("model", out var modelObj) && modelObj != null
                    ? modelObj.ToString()
                    : null;

                // Validate parameters
                if (string.IsNullOrWhiteSpace(solution))
                {
                    return McpToolResult.Error("Solution cannot be empty");
                }

                // Create the prompt
                var contextSection = string.IsNullOrWhiteSpace(context) ? "" : $"\nContext/Requirements: {context}";
                var prompt = $@"I need you to critically evaluate the following solution or approach.
Identify potential issues, weaknesses, and areas for improvement.{contextSection}

Solution/Approach: {solution}

Format your response as follows:
Summary: [Brief summary of the solution]

Strengths:
1. [Strength 1]
2. [Strength 2]
...

Weaknesses:
1. [Weakness 1]
2. [Weakness 2]
...

Potential Issues:
1. [Issue 1]
2. [Issue 2]
...

Suggested Improvements:
1. [Improvement 1]
2. [Improvement 2]
...

Overall Assessment: [Your overall assessment of the solution]

Be thorough, balanced, and constructive in your critique.";

                // Send the prompt to the LLM
                var response = await _llmService.SendMessageAsync(prompt);
                var responseText = response.Message.Content;

                // Parse the response
                var summary = "";
                var strengths = new List<string>();
                var weaknesses = new List<string>();
                var issues = new List<string>();
                var improvements = new List<string>();
                var assessment = "";

                var lines = responseText.Split('\n');
                var currentSection = "";
                var currentList = new List<string>();

                foreach (var line in lines)
                {
                    if (line.StartsWith("Summary:"))
                    {
                        currentSection = "summary";
                        summary = line.Substring("Summary:".Length).Trim();
                    }
                    else if (line.StartsWith("Strengths:"))
                    {
                        currentSection = "strengths";
                        currentList = strengths;
                    }
                    else if (line.StartsWith("Weaknesses:"))
                    {
                        currentSection = "weaknesses";
                        currentList = weaknesses;
                    }
                    else if (line.StartsWith("Potential Issues:"))
                    {
                        currentSection = "issues";
                        currentList = issues;
                    }
                    else if (line.StartsWith("Suggested Improvements:"))
                    {
                        currentSection = "improvements";
                        currentList = improvements;
                    }
                    else if (line.StartsWith("Overall Assessment:"))
                    {
                        currentSection = "assessment";
                        assessment = line.Substring("Overall Assessment:".Length).Trim();
                    }
                    else if (currentSection == "summary")
                    {
                        summary += " " + line.Trim();
                    }
                    else if (currentSection == "assessment")
                    {
                        assessment += " " + line.Trim();
                    }
                    else if (currentSection == "strengths" || currentSection == "weaknesses" ||
                             currentSection == "issues" || currentSection == "improvements")
                    {
                        var trimmedLine = line.Trim();
                        if (trimmedLine.Length > 0)
                        {
                            // Check if it's a new list item
                            if (char.IsDigit(trimmedLine[0]) && trimmedLine.Length > 1 && trimmedLine[1] == '.')
                            {
                                currentList.Add(trimmedLine.Substring(trimmedLine.IndexOf('.') + 1).Trim());
                            }
                            else if (currentList.Count > 0)
                            {
                                // Append to the last item
                                currentList[currentList.Count - 1] += " " + trimmedLine;
                            }
                        }
                    }
                }

                var result = new
                {
                    solution,
                    context,
                    summary,
                    strengths = strengths.ToArray(),
                    weaknesses = weaknesses.ToArray(),
                    potential_issues = issues.ToArray(),
                    suggested_improvements = improvements.ToArray(),
                    overall_assessment = assessment,
                    full_response = responseText
                };

                return McpToolResult.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing self-critique");
                return McpToolResult.Error($"Error executing self-critique: {ex.Message}");
            }
        }
    }
}
