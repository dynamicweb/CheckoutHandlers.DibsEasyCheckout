﻿using Dynamicweb.Core;
using Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dynamicweb.Ecommerce.CheckoutHandlers.DibsEasyCheckout.Service;

/// <summary>
/// Send request to Dibs and get response operations.
/// </summary>
internal static class DibsRequest
{
    public static string SendRequest(string apiUrl, string secretKey, CommandConfiguration configuration)
    {
        using (var messageHandler = GetMessageHandler())
        {
            using (var client = new HttpClient(messageHandler))
            {
                client.BaseAddress = new Uri(apiUrl);
                client.Timeout = new TimeSpan(0, 0, 0, 90);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(secretKey);

                string apiCommand = GetCommandLink(apiUrl, configuration.CommandType, configuration.OperatorId);
                Task<HttpResponseMessage> requestTask = configuration.CommandType switch
                {
                    //POST
                    ApiCommand.CancelPayment or
                    ApiCommand.CapturePayment or
                    ApiCommand.CreatePayment or
                    ApiCommand.RefundPayment => client.PostAsync(apiCommand, GetContent()),
                    //GET                 
                    ApiCommand.GetPayment => client.GetAsync(apiCommand),
                    _ => throw new NotSupportedException($"Unknown operation was used. The operation code: {configuration.CommandType}.")
                };

                try
                {
                    using (HttpResponseMessage response = requestTask.GetAwaiter().GetResult())
                    {
                        string data = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorResponse = Converter.Deserialize<ErrorResponse>(data);
                            if (errorResponse?.Errors?.Any() is true)
                            {
                                var errorMessage = new StringBuilder();
                                foreach ((string propertyName, IEnumerable<string> errors) in errorResponse.Errors)
                                {
                                    string errorsText = string.Join(", ", errors);
                                    errorMessage.AppendLine($"{propertyName}: {errorsText}");
                                }
                                throw new Exception(errorMessage.ToString());
                            }

                            throw new Exception($"Unhandled exception. Operation failed: {response.ReasonPhrase}");
                        }

                        return data;
                    }
                }
                catch (HttpRequestException requestException)
                {
                    throw new Exception($"An error occurred during Dibs request. Error code: {requestException.StatusCode}");
                }
            }
        }

        HttpMessageHandler GetMessageHandler() => new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
        };

        HttpContent GetContent()
        {
            string content = Converter.SerializeCompact(configuration.Data);
            return new StringContent(content, Encoding.UTF8, "application/json");
        }
    }

    private static string GetCommandLink(string baseAddress, ApiCommand command, string operatorId)
    {
        return command switch
        {
            ApiCommand.CreatePayment => GetCommandLink("payments"),
            ApiCommand.GetPayment => GetCommandLink($"payments/{operatorId}"),
            ApiCommand.CancelPayment => GetCommandLink($"payments/{operatorId}/cancels"),
            ApiCommand.CapturePayment => GetCommandLink($"payments/{operatorId}/charges"),
            ApiCommand.RefundPayment => GetCommandLink($"charges/{operatorId}/refunds"),
            _ => throw new NotSupportedException($"The api command is not supported. Command: {command}")
        };

        string GetCommandLink(string gateway) => $"{baseAddress}/v1/{gateway}";
    }
}
