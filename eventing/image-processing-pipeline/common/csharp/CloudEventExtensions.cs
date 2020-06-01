// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using CloudNative.CloudEvents;

namespace Common
{
    public static class CloudEventExtensions
    {
        public static string GetLog(this CloudEvent cloudEvent)
        {
            return $"ID: {cloudEvent.Id}\n"
                + $"Source: {cloudEvent.Source}\n"
                + $"Type: {cloudEvent.Type}\n"
                + $"Subject: {cloudEvent.Subject}\n"
                + $"DataSchema: {cloudEvent.DataSchema}\n"
                + $"DataContentType: {cloudEvent.DataContentType}\n"
                + $"Time: {cloudEvent.Time?.ToUniversalTime():yyyy-MM-dd'T'HH:mm:ss.fff'Z'}\n"
                + $"SpecVersion: {cloudEvent.SpecVersion}\n"
                + $"Data: {cloudEvent.Data}";
        }
    }
}