﻿using System;
using System.Collections.Generic;
using MetaPatterns.Abstractions;
using MetaPatterns.Tests.Repo;
using NUnit.Framework;
using Shouldly;

namespace MetaPatterns.Tests.Examples
{
    [TestFixture]
    public class ExampleTests
    {
        [Test]
        public void CanGenerateObjectByAutomaticPropertyTemplate()
        {
            //-- arrange

            var compiler = new ExampleAutomaticPropertyCompiler(new Net45MetaPatternsPlatform());

            ////-- act

            //IHaveScalarProperties obj = factory.CreateInstance<IHaveScalarProperties>();
            //obj.IntValue = 123;
            //obj.StringValue = "ABC";
            //obj.EnumValue = DayOfWeek.Thursday;
            //obj.TimeSpanValue = TimeSpan.FromSeconds(123);

            ////-- assert

            //obj.IntValue.ShouldBe(123);
            //obj.StringValue.ShouldBe("ABC");
            //obj.EnumValue.ShouldBe(DayOfWeek.Thursday);
            //obj.TimeSpanValue.ShouldBe(TimeSpan.FromSeconds(123));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public class ExampleAutomaticPropertyCompiler : MetaPatternCompiler
        {
            public ExampleAutomaticPropertyCompiler(IMetaPatternCompilerPlatform platform)
                : base(platform)
            {
            }

            //-------------------------------------------------------------------------------------------------------------------------------------------------

            protected override IMetaPatternTemplate[] BuildPipeline(MetaPatternCompilerContext context)
            {
                return new IMetaPatternTemplate[] {
                    new ExampleAutomaticProperty()
                };
            }
        }
    }
}