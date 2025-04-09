using Adept.Core.Interfaces;
using Adept.Core.Models;
using Adept.Services.Llm;
using Adept.TestUtilities.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Adept.Llm.ManualTests
{
    /// <summary>
    /// Manual tests for LLM integration
    /// </summary>
    class Program
    {
        private static IServiceProvider _serviceProvider = null!;
        private static ILogger<Program> _logger = null!;
        private static List<ILlmProvider> _providers = new();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Adept LLM Integration Manual Tests");
            Console.WriteLine("==================================");

            // Initialize services
            InitializeServices();

            // Get services
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
            _providers = new List<ILlmProvider>
            {
                new MockLlmProvider("OpenAI"),
                new MockLlmProvider("Anthropic"),
                new MockLlmProvider("Google"),
                new MockLlmProvider("Meta")
            };

            // Display menu
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nLLM Integration Test Menu:");
                Console.WriteLine("1. Test Model Selection");
                Console.WriteLine("2. Test Message Sending");
                Console.WriteLine("3. Test Streaming");
                Console.WriteLine("4. Test Tool Calls");
                Console.WriteLine("5. Test Provider Fallback");
                Console.WriteLine("0. Exit");
                Console.Write("\nSelect an option: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await TestModelSelectionAsync();
                            break;
                        case "2":
                            await TestMessageSendingAsync();
                            break;
                        case "3":
                            await TestStreamingAsync();
                            break;
                        case "4":
                            await TestToolCallsAsync();
                            break;
                        case "5":
                            await TestProviderFallbackAsync();
                            break;
                        case "0":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during test execution");
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            Console.WriteLine("\nTests completed. Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Initialize the services
        /// </summary>
        private static void InitializeServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Test model selection
        /// </summary>
        private static async Task TestModelSelectionAsync()
        {
            Console.WriteLine("Testing Model Selection");
            Console.WriteLine("======================\n");

            // Get the first provider
            var provider = _providers[0];
            Console.WriteLine($"Provider: {provider.ProviderName}");

            // Display available models
            Console.WriteLine("\nAvailable models:");
            foreach (var model in provider.AvailableModels)
            {
                Console.WriteLine($"- {model.Id}: {model.Name} (Max context: {model.MaxContextLength}, Tools: {model.SupportsToolCalls}, Vision: {model.SupportsVision})");
            }

            // Display current model
            Console.WriteLine($"\nCurrent model: {provider.ModelName}");

            // Change the model
            Console.WriteLine("\nChanging model to gpt-4-turbo...");
            bool setResult = await provider.SetModelAsync("gpt-4-turbo");
            Console.WriteLine($"Result: {(setResult ? "Success" : "Failed")}");
            Console.WriteLine($"Current model is now: {provider.ModelName}");

            // Try an invalid model
            Console.WriteLine("\nTrying to set an invalid model...");
            setResult = await provider.SetModelAsync("invalid-model");
            Console.WriteLine($"Result: {(setResult ? "Success" : "Failed")}");
            Console.WriteLine($"Current model is still: {provider.ModelName}");
        }

        /// <summary>
        /// Test message sending
        /// </summary>
        private static async Task TestMessageSendingAsync()
        {
            Console.WriteLine("Testing Message Sending");
            Console.WriteLine("======================\n");

            // Get the first provider
            var provider = _providers[0];
            Console.WriteLine($"Provider: {provider.ProviderName}");
            Console.WriteLine($"Model: {provider.ModelName}");

            // Create messages
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant."),
                LlmMessage.User("Hello, who are you?")
            };

            Console.WriteLine("\nSending messages:");
            foreach (var message in messages)
            {
                Console.WriteLine($"- {message.Role}: {message.Content}");
            }

            // Send messages
            Console.WriteLine("\nWaiting for response...");
            var response = await provider.SendMessagesAsync(messages, null);

            // Display response
            Console.WriteLine("\nResponse received:");
            Console.WriteLine($"- {response.Message.Role}: {response.Message.Content}");
            Console.WriteLine($"- Provider: {response.ProviderName}");
            Console.WriteLine($"- Model: {response.ModelName}");
            Console.WriteLine($"- Tokens: {response.TokenCount}");
        }

        /// <summary>
        /// Test streaming
        /// </summary>
        private static async Task TestStreamingAsync()
        {
            Console.WriteLine("Testing Streaming");
            Console.WriteLine("================\n");

            // Get the first provider
            var provider = _providers[0];
            Console.WriteLine($"Provider: {provider.ProviderName}");
            Console.WriteLine($"Model: {provider.ModelName}");

            // Create messages
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant."),
                LlmMessage.User("Write a short poem about programming.")
            };

            Console.WriteLine("\nSending messages for streaming:");
            foreach (var message in messages)
            {
                Console.WriteLine($"- {message.Role}: {message.Content}");
            }

            // Stream messages
            Console.WriteLine("\nStreaming response:");
            var cts = new CancellationTokenSource();
            await foreach (var chunk in provider.StreamMessagesAsync(messages, null, cts.Token))
            {
                Console.Write(chunk.Content);
            }

            Console.WriteLine("\n\nStreaming completed.");
        }

        /// <summary>
        /// Test tool calls
        /// </summary>
        private static async Task TestToolCallsAsync()
        {
            Console.WriteLine("Testing Tool Calls");
            Console.WriteLine("=================\n");

            // Get the first provider
            var provider = _providers[0];
            Console.WriteLine($"Provider: {provider.ProviderName}");
            Console.WriteLine($"Model: {provider.ModelName}");

            // Define tools
            var tools = new List<LlmTool>
            {
                new LlmTool
                {
                    Name = "get_weather",
                    Description = "Get the current weather for a location",
                    Parameters = new Dictionary<string, object>
                    {
                        ["location"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The location to get weather for"
                        },
                        ["unit"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["enum"] = new[] { "celsius", "fahrenheit" },
                            ["description"] = "The unit of temperature"
                        }
                    }
                },
                new LlmTool
                {
                    Name = "get_time",
                    Description = "Get the current time for a location",
                    Parameters = new Dictionary<string, object>
                    {
                        ["location"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "The location to get time for"
                        }
                    }
                }
            };

            // Create messages
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant with access to tools."),
                LlmMessage.User("What's the weather like in New York?")
            };

            Console.WriteLine("\nAvailable tools:");
            foreach (var tool in tools)
            {
                Console.WriteLine($"- {tool.Name}: {tool.Description}");
            }

            Console.WriteLine("\nSending messages with tools:");
            foreach (var message in messages)
            {
                Console.WriteLine($"- {message.Role}: {message.Content}");
            }

            // Send messages with tools
            Console.WriteLine("\nWaiting for response...");
            var response = await provider.SendMessagesWithToolsAsync(messages, tools, null);

            // Display response
            Console.WriteLine("\nResponse received:");
            Console.WriteLine($"- {response.Message.Role}: {response.Message.Content}");
            
            if (response.ToolCalls != null && response.ToolCalls.Count > 0)
            {
                Console.WriteLine("\nTool calls:");
                foreach (var toolCall in response.ToolCalls)
                {
                    Console.WriteLine($"- Tool: {toolCall.Name}");
                    Console.WriteLine($"  Arguments: {toolCall.Arguments}");
                    
                    // Simulate tool execution
                    string toolResponse = "";
                    if (toolCall.Name == "get_weather")
                    {
                        toolResponse = "The weather in New York is 72°F and sunny.";
                    }
                    else if (toolCall.Name == "get_time")
                    {
                        toolResponse = $"The current time in New York is {DateTime.Now.ToShortTimeString()}.";
                    }
                    
                    Console.WriteLine($"  Response: {toolResponse}");
                    
                    // Add tool response to messages
                    messages.Add(LlmMessage.Tool(toolResponse, toolCall.Name));
                }
                
                // Send follow-up with tool responses
                Console.WriteLine("\nSending follow-up with tool responses...");
                var followUpResponse = await provider.SendMessagesAsync(messages, null);
                
                Console.WriteLine("\nFollow-up response:");
                Console.WriteLine($"- {followUpResponse.Message.Role}: {followUpResponse.Message.Content}");
            }
        }

        /// <summary>
        /// Test provider fallback
        /// </summary>
        private static async Task TestProviderFallbackAsync()
        {
            Console.WriteLine("Testing Provider Fallback");
            Console.WriteLine("========================\n");

            // Create a list of providers with the first one set to fail
            var failingProvider = new MockLlmProvider("FailingProvider", true);
            var fallbackProvider = new MockLlmProvider("FallbackProvider");
            
            var providers = new List<ILlmProvider> { failingProvider, fallbackProvider };
            
            // Create a LLM service with the providers
            var llmService = new LlmService(
                providers,
                null, // Conversation repository not needed for this test
                null, // System prompt service not needed for this test
                null, // Tool integration service not needed for this test
                _serviceProvider.GetRequiredService<ILogger<LlmService>>());
            
            // Create messages
            var messages = new List<LlmMessage>
            {
                LlmMessage.System("You are a helpful assistant."),
                LlmMessage.User("Hello, who are you?")
            };
            
            Console.WriteLine("Providers:");
            Console.WriteLine($"- Primary: {failingProvider.ProviderName} (configured to fail)");
            Console.WriteLine($"- Fallback: {fallbackProvider.ProviderName}");
            
            Console.WriteLine("\nSending messages:");
            foreach (var message in messages)
            {
                Console.WriteLine($"- {message.Role}: {message.Content}");
            }
            
            // Send messages
            Console.WriteLine("\nWaiting for response...");
            var response = await llmService.SendMessageAsync(
                messages[1].Content,
                messages[0].Content,
                "test-conversation");
            
            // Display response
            Console.WriteLine("\nResponse received:");
            Console.WriteLine($"- {response.Message.Role}: {response.Message.Content}");
            Console.WriteLine($"- Provider: {response.ProviderName}");
            Console.WriteLine($"- Model: {response.ModelName}");
            
            // Verify fallback was used
            Console.WriteLine($"\nFallback used: {response.ProviderName == fallbackProvider.ProviderName}");
        }
    }

    /// <summary>
    /// Mock LLM provider for testing
    /// </summary>
    class MockLlmProvider : ILlmProvider
    {
        private readonly bool _shouldFail;
        private LlmModel _currentModel;

        public MockLlmProvider(string providerName, bool shouldFail = false)
        {
            ProviderName = providerName;
            _shouldFail = shouldFail;
            
            // Create available models
            AvailableModels = new List<LlmModel>
            {
                new LlmModel
                {
                    Id = "gpt-4",
                    Name = $"{providerName} GPT-4",
                    MaxContextLength = 8192,
                    SupportsToolCalls = true,
                    SupportsVision = true
                },
                new LlmModel
                {
                    Id = "gpt-4-turbo",
                    Name = $"{providerName} GPT-4 Turbo",
                    MaxContextLength = 128000,
                    SupportsToolCalls = true,
                    SupportsVision = true
                },
                new LlmModel
                {
                    Id = "gpt-3.5-turbo",
                    Name = $"{providerName} GPT-3.5 Turbo",
                    MaxContextLength = 16385,
                    SupportsToolCalls = true,
                    SupportsVision = false
                }
            };
            
            // Set default model
            _currentModel = AvailableModels[0];
        }

        public string ProviderName { get; }

        public string ModelName => _currentModel.Id;

        public LlmModel CurrentModel => _currentModel;

        public List<LlmModel> AvailableModels { get; }

        public bool HasValidApiKey => true;

        public bool RequiresApiKey => true;

        public bool SupportsStreaming => true;

        public bool SupportsToolCalls => true;

        public bool SupportsVision => true;

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task SetApiKeyAsync(string apiKey)
        {
            return Task.CompletedTask;
        }

        public Task<bool> SetModelAsync(string modelId)
        {
            var model = AvailableModels.Find(m => m.Id == modelId);
            if (model != null)
            {
                _currentModel = model;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<LlmResponse> SendMessagesAsync(IEnumerable<LlmMessage> messages, string? conversationId, CancellationToken cancellationToken = default)
        {
            if (_shouldFail)
            {
                throw new Exception($"{ProviderName} failed to send messages");
            }
            
            var response = new LlmResponse
            {
                Message = LlmMessage.Assistant($"This is a mock response from {ProviderName} using {ModelName}."),
                ProviderName = ProviderName,
                ModelName = ModelName,
                TokenCount = 50
            };
            
            return Task.FromResult(response);
        }

        public Task<LlmResponse> SendMessagesWithToolsAsync(IEnumerable<LlmMessage> messages, IEnumerable<LlmTool> tools, string? conversationId, CancellationToken cancellationToken = default)
        {
            if (_shouldFail)
            {
                throw new Exception($"{ProviderName} failed to send messages with tools");
            }
            
            // Create a mock tool call
            var toolCalls = new List<LlmToolCall>();
            var availableTools = tools.ToList();
            
            if (availableTools.Count > 0)
            {
                var tool = availableTools[0];
                toolCalls.Add(new LlmToolCall
                {
                    Name = tool.Name,
                    Arguments = "{\"location\":\"New York\",\"unit\":\"fahrenheit\"}"
                });
            }
            
            var response = new LlmResponse
            {
                Message = LlmMessage.Assistant($"I'll help you with that. Let me check."),
                ProviderName = ProviderName,
                ModelName = ModelName,
                TokenCount = 50,
                ToolCalls = toolCalls
            };
            
            return Task.FromResult(response);
        }

        public IAsyncEnumerable<LlmResponseChunk> StreamMessagesAsync(IEnumerable<LlmMessage> messages, string? conversationId, CancellationToken cancellationToken = default)
        {
            if (_shouldFail)
            {
                throw new Exception($"{ProviderName} failed to stream messages");
            }
            
            return new MockStreamingResponse(ProviderName, ModelName);
        }
    }

    /// <summary>
    /// Mock streaming response for testing
    /// </summary>
    class MockStreamingResponse : IAsyncEnumerable<LlmResponseChunk>
    {
        private readonly string _providerName;
        private readonly string _modelName;
        
        public MockStreamingResponse(string providerName, string modelName)
        {
            _providerName = providerName;
            _modelName = modelName;
        }
        
        public async IAsyncEnumerator<LlmResponseChunk> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            string[] chunks = {
                "Here's a short poem about programming:\n\n",
                "Code flows like a river,\n",
                "Logic guides its way.\n",
                "Bugs hide in the shadows,\n",
                "Debugging saves the day.\n\n",
                $"This poem was generated by {_providerName} using {_modelName}."
            };
            
            foreach (var chunk in chunks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                
                await Task.Delay(500, cancellationToken); // Simulate delay between chunks
                
                yield return new LlmResponseChunk
                {
                    Content = chunk,
                    ProviderName = _providerName,
                    ModelName = _modelName
                };
            }
        }
    }
}
