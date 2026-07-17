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

            // 4. Fallback to Google Translate public NMT API (Only for English targets or missing translations)
            try
            {
                var client = _httpClientFactory.CreateClient();
                // Safe stable timeout of 2000ms for API availability, falling back to original text if exceeded
                client.Timeout = TimeSpan.FromMilliseconds(2000); 

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
                                // Cache the translation (e.g. 24 hours, or permanent since translations are static)
                                _memoryCache.Set(cacheKey, translatedText, TimeSpan.FromDays(7));
                                return translatedText;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback to original text on any exception (no network, API failure, etc.)
            }

            return text;
        }
    }
}
