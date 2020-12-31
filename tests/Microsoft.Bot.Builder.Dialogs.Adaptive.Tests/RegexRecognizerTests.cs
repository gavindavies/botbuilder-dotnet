﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Tests;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers.Tests
{
    [CollectionDefinition("Dialogs.Adaptive.Recognizers")]
    public class RegexRecognizerTests : IClassFixture<ResourceExplorerFixture>
    {
        private readonly ResourceExplorerFixture _resourceExplorerFixture;

        private readonly string codeIntentMessageText = "intent a1 b2";

        private readonly string colorIntentMessageText = "I would like color red and orange";

        private readonly Dictionary<string, string> codeIntentTelemetryProperties = new Dictionary<string, string>()
        {
            { "AlteredText", null },
            { "TopIntent", "codeIntent" },
            { "TopIntentScore", "Microsoft.Bot.Builder.IntentScore" },
            { "Intents", "{\"codeIntent\":{\"score\":1.0,\"pattern\":\"(?<code>[a-z][0-9])\"}}" },
            {
                "Entities", 
                "{\r\n  \"code\": [\r\n    \"a1\",\r\n    \"b2\"\r\n  ],\r\n  \"$instance\": {\r\n    \"code\": [\r\n      {\r\n        \"startIndex\": 7,\r\n        \"endIndex\": 9,\r\n        \"score\": 1.0,\r\n        \"text\": \"a1\",\r\n        \"type\": \"code\",\r\n        \"resolution\": null\r\n      },\r\n      {\r\n        \"startIndex\": 10,\r\n        \"endIndex\": 12,\r\n        \"score\": 1.0,\r\n        \"text\": \"b2\",\r\n        \"type\": \"code\",\r\n        \"resolution\": null\r\n      }\r\n    ]\r\n  }\r\n}"
            },
            { "AdditionalProperties", null },
        };
        
        private readonly Dictionary<string, string> colorIntentTelemetryProperties = new Dictionary<string, string>()
        {
            { "AlteredText", null },
            { "TopIntent", "colorIntent" },
            { "TopIntentScore", "Microsoft.Bot.Builder.IntentScore" },
            { "Intents", "{\"colorIntent\":{\"score\":1.0,\"pattern\":\"(?i)(color|colour)\"}}" },
            {
                "Entities",
                "{\r\n  \"color\": [\r\n    \"red\",\r\n    \"orange\"\r\n  ],\r\n  \"$instance\": {\r\n    \"color\": [\r\n      {\r\n        \"startIndex\": 19,\r\n        \"endIndex\": 23,\r\n        \"score\": 1.0,\r\n        \"text\": \"red\",\r\n        \"type\": \"color\",\r\n        \"resolution\": null\r\n      },\r\n      {\r\n        \"startIndex\": 27,\r\n        \"endIndex\": 34,\r\n        \"score\": 1.0,\r\n        \"text\": \"orange\",\r\n        \"type\": \"color\",\r\n        \"resolution\": null\r\n      }\r\n    ]\r\n  }\r\n}"
            },
            { "AdditionalProperties", null },
        };

        public RegexRecognizerTests(ResourceExplorerFixture resourceExplorerFixture)
        {
            _resourceExplorerFixture = resourceExplorerFixture.Initialize(nameof(RegexRecognizerTests));
        }

        [Fact]
        public async Task RegexRecognizerTests_Entities()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer);
        }

        [Fact]
        public async Task RegexRecognizerTests_Intents()
        {
            var recognizer = new RegexRecognizer()
            {
                Intents = new List<IntentPattern>()
                {
                     new IntentPattern("codeIntent", "(?<code>[a-z][0-9])"),
                     new IntentPattern("colorIntent", "(?i)(color|colour)"),
                },
                Entities = new EntityRecognizerSet()
                {
                    new AgeEntityRecognizer(),
                    new ConfirmationEntityRecognizer(),
                    new CurrencyEntityRecognizer(),
                    new DateTimeEntityRecognizer(),
                    new DimensionEntityRecognizer(),
                    new EmailEntityRecognizer(),
                    new GuidEntityRecognizer(),
                    new HashtagEntityRecognizer(),
                    new IpEntityRecognizer(),
                    new MentionEntityRecognizer(),
                    new NumberEntityRecognizer(),
                    new NumberRangeEntityRecognizer(),
                    new OrdinalEntityRecognizer(),
                    new PercentageEntityRecognizer(),
                    new PhoneNumberEntityRecognizer(),
                    new TemperatureEntityRecognizer(),
                    new UrlEntityRecognizer(),
                    new RegexEntityRecognizer() { Name = "color", Pattern = "(?i)(red|green|blue|purple|orange|violet|white|black)" },
                    new RegexEntityRecognizer() { Name = "backgroundColor", Pattern = "(?i)(back|background) {color}" },
                    new RegexEntityRecognizer() { Name = "foregroundColor", Pattern = "(?i)(foreground|front) {color}" },
                }
            };

            // test with DC
            var dc = CreateContext(codeIntentMessageText);
            var result = await recognizer.RecognizeAsync(dc, dc.Context.Activity, CancellationToken.None);
            ValidateCodeIntent(result);

            var test = recognizer.LogPersonalInformation;

            // verify seed text is not exposed
            dynamic entities = result.Entities;
            Assert.Null(entities.text);
            Assert.NotNull(entities.code);

            dc = CreateContext(colorIntentMessageText);
            result = await recognizer.RecognizeAsync(dc, dc.Context.Activity, CancellationToken.None);
            ValidateColorIntent(result);

            dc = CreateContext(string.Empty);

            // test custom activity
            var activity = Activity.CreateMessageActivity();
            activity.Text = "intent a1 b2";
            activity.Locale = Culture.English;
            result = await recognizer.RecognizeAsync(dc, (Activity)activity, CancellationToken.None);
            ValidateCodeIntent(result);

            activity.Text = "I would like color red and orange";
            result = await recognizer.RecognizeAsync(dc, (Activity)activity, CancellationToken.None);
            ValidateColorIntent(result);
        }

        [Fact]
        public async Task RegexRecognizerTests_Intents_LogsTelemetry_WithLogPiiTrue()
        {
            var telemetryClient = new Mock<IBotTelemetryClient>();
            var recognizer = new RegexRecognizer()
            {
                Intents = new List<IntentPattern>()
                {
                     new IntentPattern("codeIntent", "(?<code>[a-z][0-9])"),
                     new IntentPattern("colorIntent", "(?i)(color|colour)"),
                },
                Entities = new EntityRecognizerSet()
                {
                    new AgeEntityRecognizer(),
                    new ConfirmationEntityRecognizer(),
                    new CurrencyEntityRecognizer(),
                    new DateTimeEntityRecognizer(),
                    new DimensionEntityRecognizer(),
                    new EmailEntityRecognizer(),
                    new GuidEntityRecognizer(),
                    new HashtagEntityRecognizer(),
                    new IpEntityRecognizer(),
                    new MentionEntityRecognizer(),
                    new NumberEntityRecognizer(),
                    new NumberRangeEntityRecognizer(),
                    new OrdinalEntityRecognizer(),
                    new PercentageEntityRecognizer(),
                    new PhoneNumberEntityRecognizer(),
                    new TemperatureEntityRecognizer(),
                    new UrlEntityRecognizer(),
                    new RegexEntityRecognizer() { Name = "color", Pattern = "(?i)(red|green|blue|purple|orange|violet|white|black)" },
                    new RegexEntityRecognizer() { Name = "backgroundColor", Pattern = "(?i)(back|background) {color}" },
                    new RegexEntityRecognizer() { Name = "foregroundColor", Pattern = "(?i)(foreground|front) {color}" },
                },
                TelemetryClient = telemetryClient.Object,
                LogPersonalInformation = true
            };

            // Test with DC
            var dc = CreateContext(codeIntentMessageText);
            var activity = dc.Context.Activity;
            var result = await recognizer.RecognizeAsync(dc, activity, CancellationToken.None);
            ValidateCodeIntent(result);
            ValidateTelemetry(recognizer, telemetryClient, dc, activity, result, callCount: 1);

            // Verify seed text is not exposed
            dynamic entities = result.Entities;
            Assert.Null(entities.text);
            Assert.NotNull(entities.code);

            dc = CreateContext(colorIntentMessageText);
            activity = dc.Context.Activity;
            result = await recognizer.RecognizeAsync(dc, activity, CancellationToken.None);
            ValidateColorIntent(result);
            ValidateTelemetry(recognizer, telemetryClient, dc, activity, result, callCount: 2);

            dc = CreateContext(string.Empty);

            // Test custom activity
            var customActivity = Activity.CreateMessageActivity();
            customActivity.Text = codeIntentMessageText;
            customActivity.Locale = Culture.English;
            result = await recognizer.RecognizeAsync(dc, (Activity)customActivity, CancellationToken.None);
            ValidateCodeIntent(result);
            ValidateTelemetry(recognizer, telemetryClient, dc, customActivity, result, callCount: 3);

            customActivity.Text = colorIntentMessageText;
            result = await recognizer.RecognizeAsync(dc, (Activity)customActivity, CancellationToken.None);
            ValidateColorIntent(result);
            ValidateTelemetry(recognizer, telemetryClient, dc, customActivity, result, callCount: 4);
        }

        private static void ValidateColorIntent(RecognizerResult result)
        {
            Assert.Single(result.Intents);
            Assert.Equal("colorIntent", result.Intents.Select(i => i.Key).First());

            // entity assertions from capture group
            dynamic entities = result.Entities;
            Assert.NotNull(entities.color);
            Assert.Null(entities.code);
            Assert.Equal(2, entities.color.Count);
            Assert.Equal("red", (string)entities.color[0]);
            Assert.Equal("orange", (string)entities.color[1]);
        }

        private static void ValidateCodeIntent(RecognizerResult result)
        {
            // intent assertions
            Assert.Single(result.Intents);
            Assert.Equal("codeIntent", result.Intents.Select(i => i.Key).First());

            // entity assertions from capture group
            dynamic entities = result.Entities;
            Assert.NotNull(entities.code);
            Assert.Null(entities.color);
            Assert.Equal(2, entities.code.Count);
            Assert.Equal("a1", (string)entities.code[0]);
            Assert.Equal("b2", (string)entities.code[1]);
        }

        private static DialogContext CreateContext(string text)
        {
            var activity = Activity.CreateMessageActivity();
            activity.Text = text;
            return new DialogContext(new DialogSet(), new TurnContext(new TestAdapter(), (Activity)activity), new DialogState());
        }

        private void ValidateTelemetry(RegexRecognizer recognizer, Mock<IBotTelemetryClient> telemetryClient, DialogContext dc, IActivity activity, RecognizerResult result, int callCount)
        {
            var (logPersonalInfo, error) = recognizer.LogPersonalInformation.TryGetObject(dc.State);
            var telemetryProps = telemetryClient.Invocations[callCount - 1].Arguments[1];

            telemetryClient.Verify(
                client => client.TrackEvent(
                    "RegexRecognizerResult",
                    It.Is<Dictionary<string, string>>(invocation => HasCorrectTelemetryProperties((IDictionary<string, string>)telemetryProps, activity, (bool)logPersonalInfo)),
                    It.IsAny<IDictionary<string, double>>()),
                Times.Exactly(callCount));
        }

        private bool HasCorrectTelemetryProperties(IDictionary<string, string> telemetryProperties, IActivity activity, bool logPersonalInformation)
        {
            var expectedProps = GetExpectedProps(activity, logPersonalInformation);

            if (expectedProps.Count == telemetryProperties.Count)
            {
                foreach (var entry in telemetryProperties)
                {
                    if (expectedProps.ContainsKey(entry.Key))
                    {
                        if (IsPiiProperty(entry.Key))
                        {
                            if (logPersonalInformation == false)
                            {
                                return false;
                            }

                            if (!HasCorrectPiiValue(telemetryProperties))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (entry.Value != expectedProps[entry.Key])
                            {
                                return false;
                            }
                        }
                    } 
                    else
                    {
                        // Telemetry logged a property that was unexpected
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        private Dictionary<string, string> GetExpectedProps(IActivity activity, bool logPersonalInformation)
        {
            var expectedProps = new Dictionary<string, string>();
            var text = activity.AsMessageActivity().Text;

            if (text == codeIntentMessageText)
            {
                expectedProps = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(codeIntentTelemetryProperties));
            }

            if (text == colorIntentMessageText)
            {
                expectedProps = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(colorIntentTelemetryProperties));
            }

            if (logPersonalInformation == true)
            {
                expectedProps.Add("Text", activity.AsMessageActivity().Text);
            }

            return expectedProps;
        }

        private bool IsPiiProperty(string telemetryProperty)
        {
            // In the future, should consider also including AlteredText
            return telemetryProperty == "Text";
        }

        private bool HasCorrectPiiValue(IDictionary<string, string> telemetryProperties)
        {
            return telemetryProperties.ContainsKey("Text") && (telemetryProperties["Text"] == codeIntentMessageText || telemetryProperties["Text"] == colorIntentMessageText);
        }
    }
}
