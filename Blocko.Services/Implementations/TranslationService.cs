using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Blocko.Services.Implementations
{
    public class TranslationService : ITranslationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _memoryCache;

        // Custom local overrides dictionary for exact terminology
        private static readonly Dictionary<string, string> ArToEnOverrides = new(StringComparer.OrdinalIgnoreCase)
        {
            { "طوب خرساني", "Concrete Blocks" },
            { "طوب خرساني 20", "Concrete Blocks 20" },
            { "إسمنت بورتلاندي", "Portland Cement" },
            { "حديد تسليح", "Rebar" },
            { "حصمة", "Aggregates" },
            { "رمل صويلح", "Sweileh Sand" },
            { "رمل", "Sand" },
            { "رمل أحمر", "Red Sand" },
            { "رمل وحصمة", "Sand & Aggregates" },
            { "طوب", "Blocks" },
            { "إسمنت", "Cement" },
            { "حديد", "Rebar" },
            { "عازل رطوبة", "Moisture Insulation" },
            { "خرسانة جاهزة", "Ready-Mix Concrete" },
            { "بلاط", "Tiles" },
            { "دهان", "Paint" },
            { "عزل", "Insulation" },
            { "أدوات صحية", "Sanitary Ware" },
            { "تأسيس كهرباء", "Electrical Rough-in" },
            { "تأسيس سباكة", "Plumbing Rough-in" },
            { "تجربة", "Test" },
            { "رمل ناعم", "Fine Sand" },
            { "حصمة فولية", "Pea Gravel" },
            { "إسمنت مقاوم الكبريتات", "Sulfate Resistant Cement" },
            { "حديد تسليح تركي", "Turkish Rebar" },
            { "د.أ", "JOD" },
            { "أسمنت الراجحي", "Al Rajhi Cement" },
            { "طن", "Ton" },
            { "متر مكعب", "Cubic Meter" }
        };

        private static readonly Dictionary<string, string> EnToArOverrides = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Concrete Blocks", "طوب خرساني" },
            { "Concrete Blocks 20", "طوب خرساني 20" },
            { "Portland Cement", "إسمنت بورتلاندي" },
            { "Rebar", "حديد تسليح" },
            { "Aggregates", "حصمة" },
            { "Sweileh Sand", "رمل صويلح" },
            { "Sand", "رمل" },
            { "Red Sand", "رمل أحمر" },
            { "Sand & Aggregates", "رمل وحصمة" },
            { "Blocks", "طوب" },
            { "Cement", "إسمنت" },
            { "Moisture Insulation", "عازل رطوبة" },
            { "Ready-Mix Concrete", "خرسانة جاهزة" },
            { "Tiles", "بلاط" },
            { "Paint", "دهان" },
            { "Insulation", "عزل" },
            { "Sanitary Ware", "أدوات صحية" },
            { "Electrical Rough-in", "تأسيس كهرباء" },
            { "Plumbing Rough-in", "تأسيس سباكة" },
            { "Test", "تجربة" },
            { "Fine Sand", "رمل ناعم" },
            { "Pea Gravel", "حصمة فولية" },
            { "Sulfate Resistant Cement", "إسمنت مقاوم الكبريتات" },
            { "Turkish Rebar", "حديد تسليح تركي" },
            { "JOD", "د.أ" },
            { "Al Rajhi Cement", "أسمنت الراجحي" },
            { "Ton", "طن" },
            { "Cubic Meter", "متر مكعب" }
        };

        public TranslationService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _memoryCache = memoryCache;
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Trim();
            targetLanguage = targetLanguage.ToLower().Split('-')[0]; // Simplify "en-US" to "en"

            // 1. Check local overrides first (Highest priority, instant response)
            if (targetLanguage == "en" && ArToEnOverrides.TryGetValue(text, out var enVal))
                return enVal;
            if (targetLanguage == "ar" && EnToArOverrides.TryGetValue(text, out var arVal))
                return arVal;

            // 2. Optimization: If target is Arabic and text already has Arabic characters, skip API completely
            if (targetLanguage == "ar" && System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0600-\u06FF]"))
            {
                return text;
            }

            // 3. Check memory cache to prevent duplicate external HTTP calls
            string cacheKey = $"translation_{targetLanguage}_{text.GetHashCode()}";
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedTranslation) && cachedTranslation != null)
            {
                return cachedTranslation;
            }

            // 4. Fallback: Try MyMemory API (Free, doesn't block cloud IPs)
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMilliseconds(2000);

                // MyMemory API expects languages in ISO format (e.g., ar|en)
                string pair = $"ar|{targetLanguage}";
                string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={pair}";

                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("responseData", out var responseData) && 
                        responseData.TryGetProperty("translatedText", out var translatedTextProp))
                    {
                        string translatedText = translatedTextProp.GetString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(translatedText))
                        {
                            // Check if translation returned same Arabic text (API failed/limit reached)
                            bool isStillArabic = System.Text.RegularExpressions.Regex.IsMatch(translatedText, @"[\u0600-\u06FF]");
                            if (!isStillArabic)
                            {
                                _memoryCache.Set(cacheKey, translatedText, TimeSpan.FromDays(7));
                                return translatedText;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TranslationService] MyMemory failed for '{text}': {ex.Message}");
            }

            // 5. Hard Fallback: Google Translate public API (as backup)
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromMilliseconds(1500);

                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={targetLanguage}&dt=t&q={Uri.EscapeDataString(text)}";
                
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var sentenceArray = root[0];
                        if (sentenceArray.ValueKind == JsonValueKind.Array)
                        {
                            var builder = new StringBuilder();
                            foreach (var item in sentenceArray.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.Array && item.GetArrayLength() > 0)
                                {
                                    var part = item[0].GetString();
                                    if (!string.IsNullOrEmpty(part))
                                    {
                                        builder.Append(part);
                                    }
                                }
                            }
                            
                            string translatedText = builder.ToString().Trim();
                            if (!string.IsNullOrEmpty(translatedText))
                            {
                                _memoryCache.Set(cacheKey, translatedText, TimeSpan.FromDays(7));
                                return translatedText;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TranslationService] Google failed for '{text}': {ex.Message}");
            }

            // Also cache non-success responses to avoid API spamming
            _memoryCache.Set(cacheKey, text, TimeSpan.FromMinutes(15));
            return text;
        }
    }
}
