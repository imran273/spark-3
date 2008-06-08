using System.Collections.Generic;
using System.Linq;

namespace Spark.Parser.Markup
{
	public class MarkupGrammar : CharGrammar
	{

		public MarkupGrammar()
		{
			var Apos = Ch('\'');
			var Quot = Ch('\"');
			var Lt = Ch('<');
			var Gt = Ch('>');


			//var CombiningChar = Ch('*');
			//var Extener = Ch('*');

			//[4]   	NameChar	   ::=   	 Letter | Digit | '.' | '-' | '_' | ':' | CombiningChar | Extender  
			var NameChar = Ch(char.IsLetterOrDigit).Or(Ch('.', '-', '_', ':'))/*.Or(CombiningChar).Or(Extener)*/;

			//[5]   	Name	   ::=   	(Letter | '_' | ':') (NameChar)*
			var Name =
				Ch(char.IsLetter).Or(Ch('_', ':')).And(Rep(NameChar))
				.Build(hit => hit.Left + new string(hit.Down.ToArray()));

			//[7]   	Nmtoken	   ::=   	(NameChar)+
			var NmToken =
				Rep1(NameChar)
				.Build(hit => new string(hit.ToArray()));

			//[3]   	S	   ::=   	(#x20 | #x9 | #xD | #xA)+
			Whitespace = Rep1(Ch(char.IsWhiteSpace));

			//[25]   	Eq	   ::=   	 S? '=' S?
			var Eq = Opt(Whitespace).And(Ch('=')).And(Opt(Whitespace));

			Text =
				Rep1(ChNot('&', '<', '$'))
				.Build(hit => new TextNode(hit));


			//[68]   	EntityRef	   ::=   	'&' Name ';'
			EntityRef =
				Ch('&').And(Name).And(Ch(';')).Left().Down()
				.Build(hit => new EntityNode(hit));

			//todo: understand csharp_expression
			// simply looking for an excluded char is ultimately insufficient because 
			// any ; or } could appear, for example, in a string contant

			// Syntax 1: ${csharp_expression}
			var Code1 = Ch("${").And(Rep1(ChNot('}'))).And(Ch('}')).Left().Down().Build(hit => new ExpressionNode(hit));

			// Syntax 2: $csharp_expression;
			var Code2 = Ch('$').And(Rep1(ChNot(';'))).And(Ch(';')).Left().Down().Build(hit => new ExpressionNode(hit));

			// Fallback: $ was single text character
			var Code3 = Ch('$').Build(hit => new TextNode("$"));

			Code = AsNode(Code1).Or(AsNode(Code2)).Or(AsNode(Code3));


			//[10]   	AttValue	   ::=   	'"' ([^<&"] | Reference)* '"' |  "'" ([^<&'] | Reference)* "'"
			var AttValueSingleText = Rep1(ChNot('<', '&', '\'', '$')).Build(hit => new TextNode(hit));
			var AttValueSingle = Apos.And(Rep(AsNode(AttValueSingleText).Or(AsNode(EntityRef)).Or(AsNode(Code)))).And(Apos);
			var AttValueDoubleText = Rep1(ChNot('<', '&', '\"', '$')).Build(hit => new TextNode(hit));
			var AttValueDouble = Quot.And(Rep(AsNode(AttValueDoubleText).Or(AsNode(EntityRef)).Or(AsNode(Code)))).And(Quot);
			var AttValue = AttValueSingle.Or(AttValueDouble).Left().Down();


			//[41]   	Attribute	   ::=   	 Name  Eq  AttValue  
			Attribute =
				Name.And(Eq).And(AttValue)
				.Build(hit => new AttributeNode(hit.Left.Left, hit.Down));


			//[40]   	STag	   ::=   	'<' Name (S  Attribute)* S? '>'
			//[44]   	EmptyElemTag	   ::=   	'<' Name (S  Attribute)* S? '/>'
			Element =
				Lt.And(Name).And(Rep(Whitespace.And(Attribute).Down())).And(Opt(Whitespace)).And(Opt(Ch('/'))).And(Gt)
				.Build(hit => new ElementNode(hit.Left.Left.Left.Left.Down, hit.Left.Left.Left.Down, hit.Left.Down != default(char)));

			//[42]   	ETag	   ::=   	'</' Name  S? '>'
			EndElement =
				Lt.And(Ch('/')).And(Name).And(Opt(Whitespace)).And(Gt)
				.Build(hit => new EndElementNode(hit.Left.Left.Down));

			//[15]   	Comment	   ::=   	'<!--' ((Char - '-') | ('-' (Char - '-')))* '-->'
			Comment =
				Ch("<!--").And(Rep(ChNot('-').Or(Ch('-').IfNext(ChNot('-'))))).And(Ch("-->"))
				.Build(hit => new CommentNode(hit.Left.Down));

			//[11]   	SystemLiteral	   ::=   	('"' [^"]* '"') | ("'" [^']* "'")
			var SystemLiteral =
				Quot.And(Rep(ChNot('\"'))).And(Quot).Or(Apos.And(Rep(ChNot('\''))).And(Apos))
				.Build(hit => new string(hit.Left.Down.ToArray()));

			//[13]   	PubidChar	   ::=   	#x20 | #xD | #xA | [a-zA-Z0-9] | [-'()+,./:=?;!*#@$_%]
			var PubidChar1 = Ch(char.IsLetterOrDigit).Or(Ch(" \r\n-()+,./:=?;!*#@$_%".ToArray()));
			var PubidChar2 = PubidChar1.Or(Apos);

			//[12]   	PubidLiteral	   ::=   	'"' PubidChar* '"' | "'" (PubidChar - "'")* "'"
			var PubidLiteral =
				Quot.And(Rep(PubidChar2)).And(Quot).Or(Apos.And(Rep(PubidChar1)).And(Apos))
				.Build(hit => new string(hit.Left.Down.ToArray()));

			//[75]   	ExternalID	   ::=   	'SYSTEM' S  SystemLiteral | 'PUBLIC' S PubidLiteral S SystemLiteral 
			var ExternalIDSystem =
				Ch("SYSTEM").And(Whitespace).And(SystemLiteral)
				.Build(hit => new ExternalIdInfo { ExternalIdType = hit.Left.Left, SystemId = hit.Down });
			var ExternalIDPublic =
				Ch("PUBLIC").And(Whitespace).And(PubidLiteral).And(Whitespace).And(SystemLiteral)
				.Build(hit => new ExternalIdInfo { ExternalIdType = hit.Left.Left.Left.Left, PublicId = hit.Left.Left.Down, SystemId = hit.Down });
			var ExternalID = ExternalIDSystem.Or(ExternalIDPublic);

			//[28]   	doctypedecl	   ::=   	'<!DOCTYPE' S  Name (S  ExternalID)? S? ('[' intSubset ']' S?)? '>'
			DoctypeDecl =
				Ch("<!DOCTYPE").And(Whitespace).And(Name).And(Opt(Whitespace.And(ExternalID).Down())).And(Opt(Whitespace)).And(Ch('>'))
				.Build(hit => new DoctypeNode { Name = hit.Left.Left.Left.Down, ExternalId = hit.Left.Left.Down });

			AnyNode = AsNode(Text)
				.Or(AsNode(EntityRef))
				.Or(AsNode(Element))
				.Or(AsNode(EndElement))
				.Or(AsNode(Code))
				.Or(AsNode(DoctypeDecl))
				.Or(AsNode(Comment));

			Nodes = Rep(AnyNode);
		}


		public ParseAction<IList<char>> Whitespace;

		public ParseAction<IList<Node>> Nodes;
		public ParseAction<Node> AnyNode;

		public ParseAction<DoctypeNode> DoctypeDecl;
		public ParseAction<TextNode> Text;
		public ParseAction<EntityNode> EntityRef;
		public ParseAction<Node> Code;
		public ParseAction<ElementNode> Element;
		public ParseAction<EndElementNode> EndElement;
		public ParseAction<AttributeNode> Attribute;
		public ParseAction<CommentNode> Comment;



		public ParseAction<Node> AsNode<TValue>(ParseAction<TValue> parser) where TValue : Node
		{
			return delegate(Position input)
					   {
						   var result = parser(input);
						   if (result == null) return null;
						   return new ParseResult<Node>(result.Rest, result.Value);
					   };
		}
	}
}