﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Ai.LanguageGeneration.Helpers;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Ai.LanguageGeneration.Engine
{
    internal class ActivityTextModifier : IActivityComponentModifier
    {
        public void Modify(Activity activity, ICompositeResponse response)
        {
            if (!string.IsNullOrWhiteSpace(activity.Text))
            {
                var recognizedPatterns = PatternRecognizer.Recognize(activity.Text);
                foreach (var pattern in recognizedPatterns)
                {
                    activity.Text = activity.Text.Replace(pattern, response.TemplateResolutions[pattern]);
                }
            }
        }
    }
}
