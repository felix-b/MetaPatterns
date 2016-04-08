﻿using System;

namespace AutoPatterns
{
    public static partial class MetaProgram
    {
        public static class Annotation
        {
            [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
            public class ClassTemplateAttribute : Attribute
            {
            }

            //-----------------------------------------------------------------------------------------------------------------------------------------------------

            [AttributeUsage(
                AttributeTargets.Constructor |
                AttributeTargets.Method |
                AttributeTargets.Property |
                AttributeTargets.Event |
                AttributeTargets.Field,
                AllowMultiple = false,
                Inherited = true)]
            public class MetaMemberAttribute : Attribute
            {
            }

            //-----------------------------------------------------------------------------------------------------------------------------------------------------

            [AttributeUsage(
                AttributeTargets.Method |
                AttributeTargets.Property |
                AttributeTargets.Event,
                AllowMultiple = false,
                Inherited = true)]
            public class DeclaredMemberAttribute : Attribute
            {
            }

            //-----------------------------------------------------------------------------------------------------------------------------------------------------

            [AttributeUsage(
                AttributeTargets.Constructor |
                AttributeTargets.Method |
                AttributeTargets.Property |
                AttributeTargets.Event |
                AttributeTargets.Field,
                AllowMultiple = false,
                Inherited = true)]
            public class NewMemberAttribute : Attribute
            {
            }
        }
    }
}