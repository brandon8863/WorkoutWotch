﻿namespace WorkoutWotch.Models.Parsers
{
    using System;
    using Kent.Boogaart.HelperTrinity.Extensions;
    using Sprache;
    using WorkoutWotch.Models.Actions;
    using WorkoutWotch.Services.Contracts.Container;

    internal static class ParallelActionParser
    {
        public static Parser<ParallelAction> GetParser(int indentLevel, IContainerService containerService)
        {
            if (indentLevel < 0)
            {
                throw new ArgumentException("indentLevel must be greater than or equal to 0.", "indentLevel");
            }

            containerService.AssertNotNull("containerService");

            return
                from _ in Parse.IgnoreCase("parallel:")
                from __ in Parse.WhiteSpace.Except(NewLineParser.Parser).Many()
                from ___ in NewLineParser.Parser
                from actions in ActionListParser.GetParser(indentLevel + 1, containerService)
                select new ParallelAction(actions);
        }
    }
}