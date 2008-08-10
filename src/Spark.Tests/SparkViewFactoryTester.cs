/*
   Copyright 2008 Louis DeJardin - http://whereslou.com

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rhino.Mocks;
using Spark.Compiler;
using Spark.FileSystem;
using Spark.Tests.Models;
using Spark.Tests.Stubs;

namespace Spark.Tests
{
    [TestFixture, Category("SparkViewEngine")]
    public class SparkViewFactoryTester
    {
        private MockRepository mocks;

        private StubViewFactory factory;
        private SparkViewEngine engine;
        private StringBuilder sb;

        [SetUp]
        public void Init()
        {
            // clears cache
            CompiledViewHolder.Current = null;


            engine = new SparkViewEngine("Spark.Tests.Stubs.StubSparkView", new FileSystemViewFolder("Views"));
            factory = new StubViewFactory { Engine = engine };

            sb = new StringBuilder();


            mocks = new MockRepository();

        }

        StubViewContext MakeViewContext(string viewName, string masterName)
        {
            return new StubViewContext { ControllerName = "Home", ViewName = viewName, MasterName = masterName, Output = sb };
        }

        StubViewContext MakeViewContext(string viewName, string masterName, StubViewData data)
        {
            return new StubViewContext { ControllerName = "Home", ViewName = viewName, MasterName = masterName, Output = sb, Data = data };
        }



        [Test]
        public void RenderPlainView()
        {
            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("index", null));

            mocks.VerifyAll();
        }


        [Test]
        public void ForEachTest()
        {
            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("foreach", null));

            mocks.VerifyAll();

            string content = sb.ToString();
            Assert.That(content.Contains(@"<li class=""odd"">1: foo</li>"));
            Assert.That(content.Contains(@"<li class=""even"">2: bar</li>"));
            Assert.That(content.Contains(@"<li class=""odd"">3: baaz</li>"));
        }


        [Test]
        public void GlobalSetTest()
        {

            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("globalset", null));

            mocks.VerifyAll();

            string content = sb.ToString();
            Assert.That(content.Contains("<p>default: Global set test</p>"));
            Assert.That(content.Contains("<p>7==7</p>"));
        }

        [Test]
        public void MasterTest()
        {
            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("childview", "layout"));

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<title>Standalone Index View</title>"));
            Assert.That(content.Contains("<h1>Standalone Index View</h1>"));
            Assert.That(content.Contains("<p>no header by default</p>"));
            Assert.That(content.Contains("<p>no footer by default</p>"));
        }

        [Test]
        public void CaptureNamedContent()
        {

            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("namedcontent", "layout"));

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<p>main content</p>"));
            Assert.That(content.Contains("<p>this is the header</p>"));
            Assert.That(content.Contains("<p>footer part one</p>"));
            Assert.That(content.Contains("<p>footer part two</p>"));
        }




        [Test, Ignore("Library no longer references asp.net mvc directly")]
        public void UsingHtmlHelper()
        {

            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("helpers", null));

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<p><a href=\"/Home/Sort\">Click me</a></p>"));
            Assert.That(content.Contains("<p>foo&gt;bar</p>"));
        }

        [Test]
        public void UsingPartialFile()
        {
            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("usingpartial", null));

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<li>Partial where x=\"zero\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"one\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"two\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"three\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"four\"</li>"));
        }

        [Test]
        public void UsingPartialWithRenderElement()
        {
            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("usingpartial-render-element", null));

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<li>Partial where x=\"zero\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"one\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"two\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"three\"</li>"));
            Assert.That(content.Contains("<li>Partial where x=\"four\"</li>"));
        }

        [Test]
        public void UsingPartialFileImplicit()
        {
            mocks.ReplayAll();

            factory.RenderView(MakeViewContext("usingpartialimplicit", null));

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<li class=\"odd\">one</li>"));
            Assert.That(content.Contains("<li class=\"even\">two</li>"));
        }


        [Test, Ignore("Library no longer references asp.net mvc directly")]
        public void DeclaringViewDataAccessor()
        {
            mocks.ReplayAll();
            //var comments = new[] { new Comment { Text = "foo" }, new Comment { Text = "bar" } };
            var viewContext = MakeViewContext("viewdata", null/*, new { Comments = comments, Caption = "Hello world" }*/);

            factory.RenderView(viewContext);

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<h1>Hello world</h1>"));
            Assert.That(content.Contains("<p>foo</p>"));
            Assert.That(content.Contains("<p>bar</p>"));
        }

        [Test]
        public void UsingNamespace()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("usingnamespace", null);

            factory.RenderView(viewContext);

            mocks.VerifyAll();
            string content = sb.ToString();
            Assert.That(content.Contains("<p>Foo</p>"));
            Assert.That(content.Contains("<p>Bar</p>"));
            Assert.That(content.Contains("<p>Hello</p>"));
        }

        [Test]
        public void IfElseElements()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("ifelement", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();
            Assert.That(!content.Contains("<if"));
            Assert.That(!content.Contains("<else"));

            Assert.That(content.Contains("<p>argis5</p>"));
            Assert.That(!content.Contains("<p>argis6</p>"));
            Assert.That(content.Contains("<p>argisstill5</p>"));
            Assert.That(!content.Contains("<p>argisnotstill5</p>"));
            Assert.That(!content.Contains("<p>argisnow6</p>"));
            Assert.That(content.Contains("<p>argisstillnot6</p>"));
        }


        [Test]
        public void IfElseAttributes()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("ifattribute", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();
            Assert.That(!content.Contains("<if"));
            Assert.That(!content.Contains("<else"));

            Assert.That(content.Contains("<p>argis5</p>"));
            Assert.That(!content.Contains("<p>argis6</p>"));
            Assert.That(content.Contains("<p>argisstill5</p>"));
            Assert.That(!content.Contains("<p>argisnotstill5</p>"));
            Assert.That(!content.Contains("<p>argisnow6</p>"));
            Assert.That(content.Contains("<p>argisstillnot6</p>"));
        }


        [Test]
        public void ChainingElseIfElement()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("elseifelement", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();
            ContainsInOrder(content,
                "<p>Hi Bob!</p>",
                "<p>Administrator James</p>",
                "<p>Test user.</p>",
                "<p>Anonymous user.</p>");
        }

        [Test]
        public void ChainingElseIfElement2()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("elseifelement2", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();

            ContainsInOrder(content,
                "<p>Hi Bob!</p>",
                "<p>Administrator James</p>",
                "<p>Test user.</p>",
                "<p>Anonymous user.</p>");
        }
        [Test]
        public void ChainingElseIfAttribute()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("elseifattribute", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();
            ContainsInOrder(content,
                "<p>Hi Bob!</p>",
                "<p>Administrator James</p>",
                "<p>Test user.</p>",
                "<p>Anonymous user.</p>");
        }

        static void ContainsInOrder(string content, params string[] values)
        {
            int index = 0;
            foreach (string value in values)
            {
                int nextIndex = content.IndexOf(value, index);
                Assert.GreaterOrEqual(nextIndex, 0, string.Format("Looking for {0}", value));
                index = nextIndex + value.Length;
            }
        }


        [Test]
        public void EachAttribute()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("eachattribute", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();
            ContainsInOrder(content,
                "<td>Bob</td>",
                "<td>James</td>",
                "<td>SpecialName</td>",
                "<td>Anonymous</td>");
        }

        [Test]
        public void MarkupBasedMacros()
        {
            var data = new StubViewData
                           {
                               {"username", "Bob"}, 
                               {"comments", new[] {
                                   new Comment {Text = "Alpha"},
                                   new Comment {Text = "Beta"},
                                   new Comment {Text = "Gamma"}
                               }}
                           };

            mocks.ReplayAll();
            var viewContext = MakeViewContext("macros", null, data);

            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();
            ContainsInOrder(content,
                "<p>Bob</p>", "<p>Alpha</p>",
                "<p>Bob</p>", "<p>Beta</p>",
                "<p>Bob</p>", "<p>Gamma</p>",
                "<span class=\"yadda\">Rating: 5</span>");
        }

        [Test]
        public void TestForEachIndex()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("foreachindex", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();
            ContainsInOrder(content,
                "<p>0: Alpha</p>",
                "<p>1: Beta</p>",
                "<p>2: Gamma</p>",
                "<p>3: Delta</p>",
                "<li ", "class=\"even\">Alpha</li>",
                "<li ", "class=\"odd\">Beta</li>",
                "<li ", "class=\"even\">Gamma</li>",
                "<li ", "class=\"odd\">Delta</li>"
                );

        }


        [Test]
        public void ConditionalTestElement()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("testelement", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();
            ContainsInOrder(content,
                            "<p>out-1</p>",
                            "<p>out-2</p>",
                            "<p>out-3</p>",
                            "<p>out-4</p>",
                            "<p>out-5</p>",
                            "<p>out-6</p>");

            Assert.IsFalse(content.Contains("fail"));

        }

        [Test]
        public void ConditionalTestElementNested()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("testelementnested", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString().Replace(" ", "").Replace("\r", "").Replace("\n", "");
            Assert.AreEqual("<p>a</p><p>b</p><p>c</p><p>d</p><p>e</p><p>f</p>", content);

        }

        [Test]
        public void PartialFilesCanHaveSpecialElements()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("partialspecialelements", null, new StubViewData { { "foo", "alpha" } });
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();
            ContainsInOrder(content, "Hi there, alpha.", "Hi there, alpha.");
        }

        [Test]
        public void StatementTerminatingStrings()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("statement-terminating-strings", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString().Replace(" ", "").Replace("\r", "").Replace("\n", "");
            Assert.AreEqual("<p>a:1</p><p>b:2</p><p>c:3%></p><p>d:<%4%></p><p>e:5%></p><p>f:<%6%></p>", content);

        }

        [Test]
        public void ExpressionHasVerbatimStrings()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("expression-has-verbatim-strings", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString().Replace(" ", "").Replace("\r", "").Replace("\n", "");
            Assert.AreEqual("<p>a\\\"b</p><p>c\\\"}d</p>", content);
        }

        [Test]
        public void RelativeApplicationPaths()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("relativeapplicationpaths", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();

            ContainsInOrder(content,
                "<img src=\"/TestApp/content/images/etc.png\"/>",
                "<script src=\"/TestApp/content/js/etc.js\"/>",
                "<p class=\"~/blah.css\"/>");
        }

        [Test]
        public void UseAssembly()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("useassembly", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();

            ContainsInOrder(content,
                "<p>SortByCategory</p>");
        }

        [Test]
        public void AddViewDataMoreThanOnce()
        {
            mocks.ReplayAll();
            var viewData = new StubViewData {{"comment", new Comment {Text = "Hello world"}}};
            var viewContext = MakeViewContext("addviewdatamorethanonce", null, viewData);
            factory.RenderView(viewContext);
            mocks.VerifyAll();

            string content = sb.ToString();

            ContainsInOrder(content,
                "<div>Hello world</div>",
                "<div>\r\n  Again: Hello world\r\n</div>");
        }


        [Test, ExpectedException(typeof(CompilerException))]
        public void AddViewDataDifferentTypes()
        {
            mocks.ReplayAll();
            var viewData = new StubViewData { { "comment", new Comment { Text = "Hello world" } } };
            var viewContext = MakeViewContext("addviewdatadifferenttypes", null, viewData);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
        }

        [Test]
        public void RenderPartialWithContainedContent()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("render-partial-with-contained-content", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();

            ContainsInOrder(content,
                "xbox",
                "xtop",
                "xb1",
                "xb2",
                "xb3",
                "xb4",
                "xboxcontent",
                "Hello World",
                "xbottom",
                "xb4",
                "xb3",
                "xb2",
                "xb1");
        }

        [Test]
        public void CaptureContentAsVariable()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("capture-content-as-variable", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();

            ContainsInOrder(content,
                "new-var-new-def-set-var");
        }


        [Test]
        public void CaptureContentBeforeAndAfter()
        {
            mocks.ReplayAll();
            var viewContext = MakeViewContext("capture-content-before-and-after", null);
            factory.RenderView(viewContext);
            mocks.VerifyAll();
            string content = sb.ToString();

            ContainsInOrder(content,
                "<p>beforedataafter</p>");
        }
    }
}
