// (c) Microsoft Corporation.  All rights reserved.

//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////
//
// Generated Files --- DO NOT EDIT!
//
//////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////

using System.Collections;

public class parse2AST {


public static declaration rewrite_interface_method_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "interface-method-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-method-declaration");

	case "interface-method-declaration : attributesopt newopt return-type identifier ( formal-parameter-listopt ) ;": {
		state tmp = node.rightmost;
		InputElement a8 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		formals a6 = rewrite_formal_parameter_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_return_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_newopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new interface_method_declaration(a1,a2,a3,a4,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_rank_specifiersopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "rank-specifiersopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("rank-specifiersopt");

	case "rank-specifiersopt :": {

		retval = intList.New();
		break;
		}

	case "rank-specifiersopt : rank-specifiers": {
		state tmp = node.rightmost;
		IList a1 = rewrite_rank_specifiers((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_property_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "property-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("property-declaration");

	case "property-declaration : attributesopt member-modifiersopt type member-name { accessor-declarations }": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a6 = rewrite_accessor_declarations((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		member_name a4 = rewrite_member_name((nonterminalState)tmp);;
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new property_declaration(a1,a2,a3,a4,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_accessor_body(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "accessor-body");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("accessor-body");

	case "accessor-body : block": {
		state tmp = node.rightmost;
		statement a1 = rewrite_block((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "accessor-body : ;": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = null;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_enum_body(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "enum-body");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("enum-body");

	case "enum-body : { enum-member-declarationsopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_enum_member_declarationsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}

	case "enum-body : { enum-member-declarations , }": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_enum_member_declarations((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_method_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "method-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("method-declaration");

	case "method-declaration : method-header method-body": {
		state tmp = node.rightmost;
		statement a2 = rewrite_method_body((nonterminalState)tmp);;
		tmp = tmp.below;
		method_header a1 = rewrite_method_header((nonterminalState)tmp);;

		retval = new method_declaration(a1.attrs,a1.mods,a1.ty,a1.name,a1.parms,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_post_decrement_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "post-decrement-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("post-decrement-expression");

	case "post-decrement-expression : primary-expression --": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_primary_expression((nonterminalState)tmp);;

		retval = new post_expression(a2,a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_class_type(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "class-type");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("class-type");

	case "class-type : type-name": {
		state tmp = node.rightmost;
		type a1 = rewrite_type_name((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-type : object": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new object_type();
		break;
		}

	case "class-type : string": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new string_type();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_switch_sectionsopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "switch-sectionsopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("switch-sectionsopt");

	case "switch-sectionsopt :": {

		retval = switch_sectionList.New();
		break;
		}

	case "switch-sectionsopt : switch-sections": {
		state tmp = node.rightmost;
		IList a1 = rewrite_switch_sections((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_interface_event_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "interface-event-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-event-declaration");

	case "interface-event-declaration : attributesopt newopt event type identifier ;": {
		state tmp = node.rightmost;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a4 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_newopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new interface_event_declaration(a1,a2,a4,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static int rewrite_dim_separatorsopt(nonterminalState node) {
	if (node.queue != null) {
		return (int)disambiguate.resolve(node, "dim-separatorsopt");
	}
	int retval;
	switch (node.rule) {
	default: throw new System.Exception("dim-separatorsopt");

	case "dim-separatorsopt :": {

		retval = 0;
		break;
		}

	case "dim-separatorsopt : dim-separators": {
		state tmp = node.rightmost;
		int a1 = rewrite_dim_separators((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_namespace_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "namespace-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("namespace-declaration");

	case "namespace-declaration : namespace qualified-identifier namespace-body ;opt": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_Aopt((nonterminalState)tmp);;
		tmp = tmp.below;
		namespace_body a3 = rewrite_namespace_body((nonterminalState)tmp);;
		tmp = tmp.below;
		dotted_name a2 = rewrite_qualified_identifier((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new namespace_declaration(a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static namespace_body rewrite_namespace_body(nonterminalState node) {
	if (node.queue != null) {
		return (namespace_body)disambiguate.resolve(node, "namespace-body");
	}
	namespace_body retval;
	switch (node.rule) {
	default: throw new System.Exception("namespace-body");

	case "namespace-body : { using-directivesopt namespace-member-declarationsopt }": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a3 = rewrite_namespace_member_declarationsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_using_directivesopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new namespace_body(a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_attribute_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "attribute-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-list");

	case "attribute-list : attribute": {
		state tmp = node.rightmost;
		attribute a1 = rewrite_attribute((nonterminalState)tmp);;

		retval = attributeList.New(a1);
		break;
		}

	case "attribute-list : attribute-list , attribute": {
		state tmp = node.rightmost;
		attribute a3 = rewrite_attribute((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attribute_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static using_directive rewrite_using_directive(nonterminalState node) {
	if (node.queue != null) {
		return (using_directive)disambiguate.resolve(node, "using-directive");
	}
	using_directive retval;
	switch (node.rule) {
	default: throw new System.Exception("using-directive");

	case "using-directive : using-alias-directive": {
		state tmp = node.rightmost;
		using_directive a1 = rewrite_using_alias_directive((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "using-directive : using-namespace-directive": {
		state tmp = node.rightmost;
		using_directive a1 = rewrite_using_namespace_directive((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_namespace_member_declarations(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "namespace-member-declarations");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("namespace-member-declarations");

	case "namespace-member-declarations : namespace-member-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_namespace_member_declaration((nonterminalState)tmp);;

		retval = declarationList.New(a1);
		break;
		}

	case "namespace-member-declarations : namespace-member-declarations namespace-member-declaration": {
		state tmp = node.rightmost;
		declaration a2 = rewrite_namespace_member_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_namespace_member_declarations((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static for_init rewrite_for_initializeropt(nonterminalState node) {
	if (node.queue != null) {
		return (for_init)disambiguate.resolve(node, "for-initializeropt");
	}
	for_init retval;
	switch (node.rule) {
	default: throw new System.Exception("for-initializeropt");

	case "for-initializeropt :": {

		retval = null;
		break;
		}

	case "for-initializeropt : for-initializer": {
		state tmp = node.rightmost;
		for_init a1 = rewrite_for_initializer((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_local_constant_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "local-constant-declaration");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("local-constant-declaration");

	case "local-constant-declaration : const type constant-declarators": {
		state tmp = node.rightmost;
		IList a3 = rewrite_constant_declarators((nonterminalState)tmp);;
		tmp = tmp.below;
		type a2 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new const_statement(a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_relational_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "relational-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("relational-expression");

	case "relational-expression : shift-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_shift_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "relational-expression : relational-expression < shift-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_shift_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_relational_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "relational-expression : relational-expression > shift-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_shift_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_relational_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "relational-expression : relational-expression <= shift-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_shift_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_relational_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "relational-expression : relational-expression >= shift-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_shift_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_relational_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "relational-expression : relational-expression is type": {
		state tmp = node.rightmost;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_relational_expression((nonterminalState)tmp);;

		retval = new is_expression(a1,a3);
		break;
		}

	case "relational-expression : relational-expression as type": {
		state tmp = node.rightmost;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_relational_expression((nonterminalState)tmp);;

		retval = new as_expression(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_equality_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "equality-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("equality-expression");

	case "equality-expression : relational-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_relational_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "equality-expression : equality-expression == relational-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_relational_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_equality_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "equality-expression : equality-expression != relational-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_relational_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_equality_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_for_conditionopt(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "for-conditionopt");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("for-conditionopt");

	case "for-conditionopt :": {

		retval = null;
		break;
		}

	case "for-conditionopt : for-condition": {
		state tmp = node.rightmost;
		expression a1 = rewrite_for_condition((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_iteration_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "iteration-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("iteration-statement");

	case "iteration-statement : while-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_while_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "iteration-statement : do-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_do_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "iteration-statement : for-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_for_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "iteration-statement : foreach-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_foreach_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static dotted_name rewrite_qualified_identifier(nonterminalState node) {
	if (node.queue != null) {
		return (dotted_name)disambiguate.resolve(node, "qualified-identifier");
	}
	dotted_name retval;
	switch (node.rule) {
	default: throw new System.Exception("qualified-identifier");

	case "qualified-identifier : identifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new dotted_name(null,a1);
		break;
		}

	case "qualified-identifier : qualified-identifier . identifier": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		dotted_name a1 = rewrite_qualified_identifier((nonterminalState)tmp);;

		retval = new dotted_name(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_additive_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "additive-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("additive-expression");

	case "additive-expression : multiplicative-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_multiplicative_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "additive-expression : additive-expression + multiplicative-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_multiplicative_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_additive_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "additive-expression : additive-expression - multiplicative-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_multiplicative_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_additive_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static formals rewrite_formal_parameter_listopt(nonterminalState node) {
	if (node.queue != null) {
		return (formals)disambiguate.resolve(node, "formal-parameter-listopt");
	}
	formals retval;
	switch (node.rule) {
	default: throw new System.Exception("formal-parameter-listopt");

	case "formal-parameter-listopt :": {

		retval = new formals(parameterList.New(),null);
		break;
		}

	case "formal-parameter-listopt : formal-parameter-list": {
		state tmp = node.rightmost;
		formals a1 = rewrite_formal_parameter_list((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_interface_property_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "interface-property-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-property-declaration");

	case "interface-property-declaration : attributesopt newopt type identifier { interface-accessors }": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a6 = rewrite_interface_accessors((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_newopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new interface_property_declaration(a1,a2,a3,a4,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_class_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "class-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("class-declaration");

	case "class-declaration : attributesopt member-modifiersopt class identifier class-baseopt class-body ;opt": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_Aopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a6 = rewrite_class_body((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a5 = rewrite_class_baseopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new class_declaration(a1,a2,a4,a5,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static constructor_declarator rewrite_constructor_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (constructor_declarator)disambiguate.resolve(node, "constructor-declarator");
	}
	constructor_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("constructor-declarator");

	case "constructor-declarator : identifier ( formal-parameter-listopt ) constructor-initializeropt": {
		state tmp = node.rightmost;
		constructor_initializer a5 = rewrite_constructor_initializeropt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		formals a3 = rewrite_formal_parameter_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new constructor_declarator(a1,a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_inclusive_or_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "inclusive-or-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("inclusive-or-expression");

	case "inclusive-or-expression : exclusive-or-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_exclusive_or_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "inclusive-or-expression : inclusive-or-expression | exclusive-or-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_exclusive_or_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_inclusive_or_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_cast_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "cast-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("cast-expression");

	case "cast-expression : ( type ) unary-expression": {
		state tmp = node.rightmost;
		expression a4 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a2 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new cast_expression(a2,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_enum_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "enum-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("enum-declaration");

	case "enum-declaration : attributesopt member-modifiersopt enum identifier enum-baseopt enum-body ;opt": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_Aopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a6 = rewrite_enum_body((nonterminalState)tmp);;
		tmp = tmp.below;
		type a5 = rewrite_enum_baseopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new enum_declaration(a1,a2,a4,a5,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_expression_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "expression-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("expression-statement");

	case "expression-statement : statement-expression ;": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_statement_expression((nonterminalState)tmp);;

		retval = new expression_statement(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_attribute_target(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "attribute-target");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-target");

	case "attribute-target : identifier===field": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "attribute-target : event": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "attribute-target : identifier===method": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "attribute-target : identifier===param": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "attribute-target : identifier===property": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "attribute-target : return": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "attribute-target : identifier===type": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static variable_initializer rewrite_variable_initializer(nonterminalState node) {
	if (node.queue != null) {
		return (variable_initializer)disambiguate.resolve(node, "variable-initializer");
	}
	variable_initializer retval;
	switch (node.rule) {
	default: throw new System.Exception("variable-initializer");

	case "variable-initializer : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = new expr_initializer(a1);
		break;
		}

	case "variable-initializer : array-initializer": {
		state tmp = node.rightmost;
		array_initializer a1 = rewrite_array_initializer((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "variable-initializer : stackalloc-initializer": {
		state tmp = node.rightmost;
		variable_initializer a1 = rewrite_stackalloc_initializer((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_global_attribute_target(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "global-attribute-target");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("global-attribute-target");

	case "global-attribute-target : identifier===assembly": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "global-attribute-target : identifier===module": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_typeswitch_block(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "typeswitch-block");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("typeswitch-block");

	case "typeswitch-block : { typeswitch-sectionsopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_typeswitch_sectionsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_indexer_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "indexer-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("indexer-declaration");

	case "indexer-declaration : attributesopt member-modifiersopt indexer-declarator { accessor-declarations }": {
		state tmp = node.rightmost;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a5 = rewrite_accessor_declarations((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		indexer a3 = rewrite_indexer_declarator((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new indexer_declaration(a1,a2,a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static dotted_name rewrite_namespace_name(nonterminalState node) {
	if (node.queue != null) {
		return (dotted_name)disambiguate.resolve(node, "namespace-name");
	}
	dotted_name retval;
	switch (node.rule) {
	default: throw new System.Exception("namespace-name");

	case "namespace-name : namespace-or-type-name": {
		state tmp = node.rightmost;
		dotted_name a1 = rewrite_namespace_or_type_name((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_attribute_target_specifier(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "attribute-target-specifier");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-target-specifier");

	case "attribute-target-specifier : attribute-target :": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_attribute_target((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_for_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "for-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("for-statement");

	case "for-statement : for ( for-initializeropt ; for-conditionopt ; for-iteratoropt ) embedded-statement": {
		state tmp = node.rightmost;
		statement a9 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a8 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a7 = rewrite_for_iteratoropt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a5 = rewrite_for_conditionopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		for_init a3 = rewrite_for_initializeropt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new for_statement(a3,a5,a7,a9);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_null_literal(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "null-literal");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("null-literal");

	case "null-literal : null": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new null_literal(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static finally_clause rewrite_finally_clause(nonterminalState node) {
	if (node.queue != null) {
		return (finally_clause)disambiguate.resolve(node, "finally-clause");
	}
	finally_clause retval;
	switch (node.rule) {
	default: throw new System.Exception("finally-clause");

	case "finally-clause : finally block": {
		state tmp = node.rightmost;
		statement a2 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new finally_clause(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_struct_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "struct-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("struct-declaration");

	case "struct-declaration : attributesopt member-modifiersopt struct identifier struct-interfacesopt struct-body ;opt": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_Aopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a6 = rewrite_struct_body((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a5 = rewrite_struct_interfacesopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new struct_declaration(a1,a2,a4,a5,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_this_access(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "this-access");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("this-access");

	case "this-access : this": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new this_access();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static typeswitch_section rewrite_typeswitch_section(nonterminalState node) {
	if (node.queue != null) {
		return (typeswitch_section)disambiguate.resolve(node, "typeswitch-section");
	}
	typeswitch_section retval;
	switch (node.rule) {
	default: throw new System.Exception("typeswitch-section");

	case "typeswitch-section : case type ( identifier ) : statement-list": {
		state tmp = node.rightmost;
		IList a7 = rewrite_statement_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a2 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new typeswitch_section(a2,a4,a7);
		break;
		}

	case "typeswitch-section : typeswitch-labels statement-list": {
		state tmp = node.rightmost;
		IList a2 = rewrite_statement_list((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_typeswitch_labels((nonterminalState)tmp);;

		retval = new typeswitch_section(a1,a2);
		break;
		}

	case "typeswitch-section : default : statement-list": {
		state tmp = node.rightmost;
		IList a3 = rewrite_statement_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new typeswitch_section(a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_enum_base(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "enum-base");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("enum-base");

	case "enum-base : : integral-type": {
		state tmp = node.rightmost;
		type a2 = rewrite_integral_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_Aopt(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, ";opt");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception(";opt");

	case ";opt :": {

		retval = null;
		break;
		}

	case ";opt : ;": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static accessor_declaration rewrite_get_accessor_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (accessor_declaration)disambiguate.resolve(node, "get-accessor-declaration");
	}
	accessor_declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("get-accessor-declaration");

	case "get-accessor-declaration : attributesopt identifier===get accessor-body": {
		state tmp = node.rightmost;
		statement a3 = rewrite_accessor_body((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new accessor_declaration(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_switch_labels(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "switch-labels");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("switch-labels");

	case "switch-labels : switch-label": {
		state tmp = node.rightmost;
		switch_label a1 = rewrite_switch_label((nonterminalState)tmp);;

		retval = switch_labelList.New(a1);
		break;
		}

	case "switch-labels : switch-labels switch-label": {
		state tmp = node.rightmost;
		switch_label a2 = rewrite_switch_label((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_switch_labels((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_block(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "block");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("block");

	case "block : { statement-listopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_statement_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new block_statement(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_unmanaged_type(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "unmanaged-type");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("unmanaged-type");

	case "unmanaged-type : type": {
		state tmp = node.rightmost;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_rank_specifiers(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "rank-specifiers");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("rank-specifiers");

	case "rank-specifiers : rank-specifier": {
		state tmp = node.rightmost;
		int a1 = rewrite_rank_specifier((nonterminalState)tmp);;

		retval = intList.New(a1);
		break;
		}

	case "rank-specifiers : rank-specifiers rank-specifier": {
		state tmp = node.rightmost;
		int a2 = rewrite_rank_specifier((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_rank_specifiers((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_simple_name(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "simple-name");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("simple-name");

	case "simple-name : identifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new simple_name(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_global_attributesopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "global-attributesopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("global-attributesopt");

	case "global-attributesopt :": {

		retval = attribute_sectionList.New();
		break;
		}

	case "global-attributesopt : global-attributes": {
		state tmp = node.rightmost;
		IList a1 = rewrite_global_attributes((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_statement_expression_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "statement-expression-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("statement-expression-list");

	case "statement-expression-list : statement-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_statement_expression((nonterminalState)tmp);;

		retval = expressionList.New(a1);
		break;
		}

	case "statement-expression-list : statement-expression-list , statement-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_statement_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_statement_expression_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_interface_indexer_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "interface-indexer-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-indexer-declaration");

	case "interface-indexer-declaration : attributesopt newopt type this [ formal-parameter-list ] { interface-accessors }": {
		state tmp = node.rightmost;
		InputElement a10 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a9 = rewrite_interface_accessors((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a8 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		formals a6 = rewrite_formal_parameter_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_newopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new interface_indexer_declaration(a1,a2,a3,a6,a9);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_attributes(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "attributes");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("attributes");

	case "attributes : attribute-sections": {
		state tmp = node.rightmost;
		IList a1 = rewrite_attribute_sections((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_shift_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "shift-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("shift-expression");

	case "shift-expression : additive-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_additive_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "shift-expression : shift-expression << additive-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_additive_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_shift_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "shift-expression : shift-expression >> additive-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_additive_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_shift_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_struct_interfaces(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "struct-interfaces");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("struct-interfaces");

	case "struct-interfaces : : interface-type-list": {
		state tmp = node.rightmost;
		IList a2 = rewrite_interface_type_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static var_declarator rewrite_variable_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (var_declarator)disambiguate.resolve(node, "variable-declarator");
	}
	var_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("variable-declarator");

	case "variable-declarator : identifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new var_declarator(a1,null);
		break;
		}

	case "variable-declarator : identifier = variable-initializer": {
		state tmp = node.rightmost;
		variable_initializer a3 = rewrite_variable_initializer((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new var_declarator(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static formals rewrite_formal_parameter_list(nonterminalState node) {
	if (node.queue != null) {
		return (formals)disambiguate.resolve(node, "formal-parameter-list");
	}
	formals retval;
	switch (node.rule) {
	default: throw new System.Exception("formal-parameter-list");

	case "formal-parameter-list : fixed-parameters": {
		state tmp = node.rightmost;
		IList a1 = rewrite_fixed_parameters((nonterminalState)tmp);;

		retval = new formals(a1,null);
		break;
		}

	case "formal-parameter-list : fixed-parameters , parameter-array": {
		state tmp = node.rightmost;
		parameter a3 = rewrite_parameter_array((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_fixed_parameters((nonterminalState)tmp);;

		retval = new formals(a1,a3);
		break;
		}

	case "formal-parameter-list : parameter-array": {
		state tmp = node.rightmost;
		parameter a1 = rewrite_parameter_array((nonterminalState)tmp);;

		retval = new formals(parameterList.New(),a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_delegate_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "delegate-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("delegate-declaration");

	case "delegate-declaration : attributesopt member-modifiersopt delegate return-type identifier ( formal-parameter-listopt ) ;": {
		state tmp = node.rightmost;
		InputElement a9 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a8 = rewrite_terminal(tmp);
		tmp = tmp.below;
		formals a7 = rewrite_formal_parameter_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a4 = rewrite_return_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new delegate_declaration(a1,a2,a4,a5,a7);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_interface_accessors(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "interface-accessors");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-accessors");

	case "interface-accessors : attributesopt identifier ;": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = accessor_declarationList.New(new accessor_declaration(a1,a2));
		break;
		}

	case "interface-accessors : attributesopt identifier ; attributesopt identifier ;": {
		state tmp = node.rightmost;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_attributesopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = accessor_declarationList.New(new accessor_declaration(a1,a2), new accessor_declaration(a4,a5));
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_unary_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "unary-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("unary-expression");

	case "unary-expression : primary-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_primary_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "unary-expression : + unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unary_expression(a1,a2);
		break;
		}

	case "unary-expression : - unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unary_expression(a1,a2);
		break;
		}

	case "unary-expression : ! unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unary_expression(a1,a2);
		break;
		}

	case "unary-expression : ~ unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unary_expression(a1,a2);
		break;
		}

	case "unary-expression : * unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unary_expression(a1,a2);
		break;
		}

	case "unary-expression : pre-increment-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_pre_increment_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "unary-expression : pre-decrement-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_pre_decrement_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "unary-expression : cast-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_cast_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "unary-expression : addressof-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_addressof_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_field_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "field-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("field-declaration");

	case "field-declaration : attributesopt member-modifiersopt type variable-declarators ;": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_variable_declarators((nonterminalState)tmp);;
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new field_declaration(a1,a2,a3,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_for_iterator(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "for-iterator");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("for-iterator");

	case "for-iterator : statement-expression-list": {
		state tmp = node.rightmost;
		IList a1 = rewrite_statement_expression_list((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_member_modifiersopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "member-modifiersopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("member-modifiersopt");

	case "member-modifiersopt :": {

		retval = InputElementList.New();
		break;
		}

	case "member-modifiersopt : member-modifiers": {
		state tmp = node.rightmost;
		IList a1 = rewrite_member_modifiers((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_class_base(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "class-base");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("class-base");

	case "class-base : : class-type-list": {
		state tmp = node.rightmost;
		IList a2 = rewrite_class_type_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_class_baseopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "class-baseopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("class-baseopt");

	case "class-baseopt :": {

		retval = typeList.New();
		break;
		}

	case "class-baseopt : class-base": {
		state tmp = node.rightmost;
		IList a1 = rewrite_class_base((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_pre_increment_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "pre-increment-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("pre-increment-expression");

	case "pre-increment-expression : ++ unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new pre_expression(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_assignment_operator(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "assignment-operator");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("assignment-operator");

	case "assignment-operator : +=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : -=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : *=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : /=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : %=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : &=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : |=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : ^=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : <<=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "assignment-operator : >>=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_literal(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "literal");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("literal");

	case "literal : boolean-literal": {
		state tmp = node.rightmost;
		expression a1 = rewrite_boolean_literal((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "literal : integer-literal": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new integer_literal(a1);
		break;
		}

	case "literal : real-literal": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new real_literal(a1);
		break;
		}

	case "literal : character-literal": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new character_literal(a1);
		break;
		}

	case "literal : string-literal": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new string_literal(a1);
		break;
		}

	case "literal : null-literal": {
		state tmp = node.rightmost;
		expression a1 = rewrite_null_literal((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_interface_body(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "interface-body");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-body");

	case "interface-body : { interface-member-declarationsopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_interface_member_declarationsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_event_accessor_declarations(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "event-accessor-declarations");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("event-accessor-declarations");

	case "event-accessor-declarations : remove-accessor-declaration add-accessor-declaration": {
		state tmp = node.rightmost;
		event_accessor a2 = rewrite_add_accessor_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		event_accessor a1 = rewrite_remove_accessor_declaration((nonterminalState)tmp);;

		retval = event_accessorList.New(a1,a2);
		break;
		}

	case "event-accessor-declarations : add-accessor-declaration remove-accessor-declaration": {
		state tmp = node.rightmost;
		event_accessor a2 = rewrite_remove_accessor_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		event_accessor a1 = rewrite_add_accessor_declaration((nonterminalState)tmp);;

		retval = event_accessorList.New(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_local_variable_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "local-variable-declaration");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("local-variable-declaration");

	case "local-variable-declaration : type variable-declarators": {
		state tmp = node.rightmost;
		IList a2 = rewrite_variable_declarators((nonterminalState)tmp);;
		tmp = tmp.below;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = new local_statement(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_attributesopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "attributesopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("attributesopt");

	case "attributesopt :": {

		retval = attribute_sectionList.New();
		break;
		}

	case "attributesopt : attributes": {
		state tmp = node.rightmost;
		IList a1 = rewrite_attributes((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_fixed_pointer_declarators(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "fixed-pointer-declarators");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("fixed-pointer-declarators");

	case "fixed-pointer-declarators : fixed-pointer-declarator": {
		state tmp = node.rightmost;
		fixed_declarator a1 = rewrite_fixed_pointer_declarator((nonterminalState)tmp);;

		retval = declaratorList.New(a1);
		break;
		}

	case "fixed-pointer-declarators : fixed-pointer-declarators , fixed-pointer-declarator": {
		state tmp = node.rightmost;
		fixed_declarator a3 = rewrite_fixed_pointer_declarator((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_fixed_pointer_declarators((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_using_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "using-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("using-statement");

	case "using-statement : using ( resource-acquisition ) embedded-statement": {
		state tmp = node.rightmost;
		statement a5 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		resource a3 = rewrite_resource_acquisition((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new using_statement(a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_accessor_declarations(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "accessor-declarations");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("accessor-declarations");

	case "accessor-declarations : get-accessor-declaration": {
		state tmp = node.rightmost;
		accessor_declaration a1 = rewrite_get_accessor_declaration((nonterminalState)tmp);;

		retval = accessor_declarationList.New(a1);
		break;
		}

	case "accessor-declarations : get-accessor-declaration set-accessor-declaration": {
		state tmp = node.rightmost;
		accessor_declaration a2 = rewrite_set_accessor_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		accessor_declaration a1 = rewrite_get_accessor_declaration((nonterminalState)tmp);;

		retval = accessor_declarationList.New(a1,a2);
		break;
		}

	case "accessor-declarations : set-accessor-declaration": {
		state tmp = node.rightmost;
		accessor_declaration a1 = rewrite_set_accessor_declaration((nonterminalState)tmp);;

		retval = accessor_declarationList.New(a1);
		break;
		}

	case "accessor-declarations : set-accessor-declaration get-accessor-declaration": {
		state tmp = node.rightmost;
		accessor_declaration a2 = rewrite_get_accessor_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		accessor_declaration a1 = rewrite_set_accessor_declaration((nonterminalState)tmp);;

		retval = accessor_declarationList.New(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_constructor_body(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "constructor-body");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("constructor-body");

	case "constructor-body : block": {
		state tmp = node.rightmost;
		statement a1 = rewrite_block((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "constructor-body : ;": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = null;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_interface_member_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "interface-member-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-member-declaration");

	case "interface-member-declaration : interface-method-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_interface_method_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "interface-member-declaration : interface-property-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_interface_property_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "interface-member-declaration : interface-event-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_interface_event_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "interface-member-declaration : interface-indexer-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_interface_indexer_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_return_type(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "return-type");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("return-type");

	case "return-type : type": {
		state tmp = node.rightmost;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "return-type : void": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new void_type();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_positional_argument(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "positional-argument");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("positional-argument");

	case "positional-argument : attribute-argument-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_attribute_argument_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static constructor_initializer rewrite_constructor_initializer(nonterminalState node) {
	if (node.queue != null) {
		return (constructor_initializer)disambiguate.resolve(node, "constructor-initializer");
	}
	constructor_initializer retval;
	switch (node.rule) {
	default: throw new System.Exception("constructor-initializer");

	case "constructor-initializer : : base ( argument-listopt )": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_argument_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new base_initializer(a4);
		break;
		}

	case "constructor-initializer : : this ( argument-listopt )": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_argument_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new this_initializer(a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_attribute_sections(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "attribute-sections");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-sections");

	case "attribute-sections : attribute-section": {
		state tmp = node.rightmost;
		attribute_section a1 = rewrite_attribute_section((nonterminalState)tmp);;

		retval = attribute_sectionList.New(a1);
		break;
		}

	case "attribute-sections : attribute-sections attribute-section": {
		state tmp = node.rightmost;
		attribute_section a2 = rewrite_attribute_section((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attribute_sections((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_fixed_parameters(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "fixed-parameters");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("fixed-parameters");

	case "fixed-parameters : fixed-parameter": {
		state tmp = node.rightmost;
		fixed_parameter a1 = rewrite_fixed_parameter((nonterminalState)tmp);;

		retval = parameterList.New(a1);
		break;
		}

	case "fixed-parameters : fixed-parameters , fixed-parameter": {
		state tmp = node.rightmost;
		fixed_parameter a3 = rewrite_fixed_parameter((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_fixed_parameters((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_struct_interfacesopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "struct-interfacesopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("struct-interfacesopt");

	case "struct-interfacesopt :": {

		retval = typeList.New();
		break;
		}

	case "struct-interfacesopt : struct-interfaces": {
		state tmp = node.rightmost;
		IList a1 = rewrite_struct_interfaces((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_enum_member_declarationsopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "enum-member-declarationsopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("enum-member-declarationsopt");

	case "enum-member-declarationsopt :": {

		retval = enum_member_declarationList.New();
		break;
		}

	case "enum-member-declarationsopt : enum-member-declarations": {
		state tmp = node.rightmost;
		IList a1 = rewrite_enum_member_declarations((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_namespace_member_declarationsopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "namespace-member-declarationsopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("namespace-member-declarationsopt");

	case "namespace-member-declarationsopt :": {

		retval = declarationList.New();;
		break;
		}

	case "namespace-member-declarationsopt : namespace-member-declarations": {
		state tmp = node.rightmost;
		IList a1 = rewrite_namespace_member_declarations((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_array_type(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "array-type");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("array-type");

	case "array-type : type rank-specifier": {
		state tmp = node.rightmost;
		int a2 = rewrite_rank_specifier((nonterminalState)tmp);;
		tmp = tmp.below;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = new array_type(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_attribute_name(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "attribute-name");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-name");

	case "attribute-name : type-name": {
		state tmp = node.rightmost;
		type a1 = rewrite_type_name((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_interface_base(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "interface-base");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-base");

	case "interface-base : : interface-type-list": {
		state tmp = node.rightmost;
		IList a2 = rewrite_interface_type_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_type_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "type-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("type-declaration");

	case "type-declaration : class-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_class_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "type-declaration : struct-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_struct_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "type-declaration : interface-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_interface_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "type-declaration : enum-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_enum_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "type-declaration : delegate-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_delegate_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_continue_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "continue-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("continue-statement");

	case "continue-statement : continue ;": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new continue_statement();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_goto_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "goto-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("goto-statement");

	case "goto-statement : goto identifier ;": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new goto_statement(a2);
		break;
		}

	case "goto-statement : goto case constant-expression ;": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_constant_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new goto_case_statement(a3);
		break;
		}

	case "goto-statement : goto default ;": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new goto_default_statement();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_attribute_target_specifieropt(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "attribute-target-specifieropt");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-target-specifieropt");

	case "attribute-target-specifieropt :": {

		retval = null;
		break;
		}

	case "attribute-target-specifieropt : attribute-target-specifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_attribute_target_specifier((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static resource rewrite_resource_acquisition(nonterminalState node) {
	if (node.queue != null) {
		return (resource)disambiguate.resolve(node, "resource-acquisition");
	}
	resource retval;
	switch (node.rule) {
	default: throw new System.Exception("resource-acquisition");

	case "resource-acquisition : local-variable-declaration": {
		state tmp = node.rightmost;
		statement a1 = rewrite_local_variable_declaration((nonterminalState)tmp);;

		retval = new resource_decl((local_statement)a1);
		break;
		}

	case "resource-acquisition : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = new resource_expr(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_primary_expression_no_array_creation(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "primary-expression-no-array-creation");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("primary-expression-no-array-creation");

	case "primary-expression-no-array-creation : literal": {
		state tmp = node.rightmost;
		expression a1 = rewrite_literal((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : simple-name": {
		state tmp = node.rightmost;
		expression a1 = rewrite_simple_name((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : parenthesized-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_parenthesized_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : member-access": {
		state tmp = node.rightmost;
		expression a1 = rewrite_member_access((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : invocation-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_invocation_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : element-access": {
		state tmp = node.rightmost;
		expression a1 = rewrite_element_access((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : this-access": {
		state tmp = node.rightmost;
		expression a1 = rewrite_this_access((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : base-access": {
		state tmp = node.rightmost;
		expression a1 = rewrite_base_access((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : post-increment-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_post_increment_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : post-decrement-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_post_decrement_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : new-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_new_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : typeof-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_typeof_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : sizeof-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_sizeof_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : checked-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_checked_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression-no-array-creation : unchecked-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_unchecked_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_interface_member_declarationsopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "interface-member-declarationsopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-member-declarationsopt");

	case "interface-member-declarationsopt :": {

		retval = declarationList.New();
		break;
		}

	case "interface-member-declarationsopt : interface-member-declarations": {
		state tmp = node.rightmost;
		IList a1 = rewrite_interface_member_declarations((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_variable_reference(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "variable-reference");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("variable-reference");

	case "variable-reference : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static int rewrite_dim_separators(nonterminalState node) {
	if (node.queue != null) {
		return (int)disambiguate.resolve(node, "dim-separators");
	}
	int retval;
	switch (node.rule) {
	default: throw new System.Exception("dim-separators");

	case "dim-separators : ,": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = 1;
		break;
		}

	case "dim-separators : dim-separators ,": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		int a1 = rewrite_dim_separators((nonterminalState)tmp);;

		retval = a1+1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_struct_body(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "struct-body");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("struct-body");

	case "struct-body : { struct-member-declarationsopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_struct_member_declarationsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static operator_declarator rewrite_binary_operator_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (operator_declarator)disambiguate.resolve(node, "binary-operator-declarator");
	}
	operator_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("binary-operator-declarator");

	case "binary-operator-declarator : type operator overloadable-binary-operator ( type identifier , type identifier )": {
		state tmp = node.rightmost;
		InputElement a10 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a9 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a8 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a5 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_overloadable_binary_operator((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = new binary_declarator(a1,a3,a5,a6,a8,a9);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_integral_type(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "integral-type");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("integral-type");

	case "integral-type : sbyte": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new sbyte_type();
		break;
		}

	case "integral-type : byte": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new byte_type();
		break;
		}

	case "integral-type : short": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new short_type();
		break;
		}

	case "integral-type : ushort": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new ushort_type();
		break;
		}

	case "integral-type : int": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new int_type();
		break;
		}

	case "integral-type : uint": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new uint_type();
		break;
		}

	case "integral-type : long": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new long_type();
		break;
		}

	case "integral-type : ulong": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new ulong_type();
		break;
		}

	case "integral-type : char": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new char_type();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_typeswitch_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "typeswitch-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("typeswitch-statement");

	case "typeswitch-statement : typeswitch ( expression ) typeswitch-block": {
		state tmp = node.rightmost;
		IList a5 = rewrite_typeswitch_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new typeswitch_statement(a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static int rewrite_rank_specifier(nonterminalState node) {
	if (node.queue != null) {
		return (int)disambiguate.resolve(node, "rank-specifier");
	}
	int retval;
	switch (node.rule) {
	default: throw new System.Exception("rank-specifier");

	case "rank-specifier : [ dim-separatorsopt ]": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		int a2 = rewrite_dim_separatorsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_array_creation_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "array-creation-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("array-creation-expression");

	case "array-creation-expression : new type [ expression-list ] rank-specifiersopt array-initializeropt": {
		state tmp = node.rightmost;
		array_initializer a7 = rewrite_array_initializeropt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a6 = rewrite_rank_specifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_expression_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a2 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new array_creation_expression1(a2,a4,a6,a7);
		break;
		}

	case "array-creation-expression : new array-type array-initializer": {
		state tmp = node.rightmost;
		array_initializer a3 = rewrite_array_initializer((nonterminalState)tmp);;
		tmp = tmp.below;
		type a2 = rewrite_array_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new array_creation_expression2(a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_class_member_declarationsopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "class-member-declarationsopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("class-member-declarationsopt");

	case "class-member-declarationsopt :": {

		retval = declarationList.New();
		break;
		}

	case "class-member-declarationsopt : class-member-declarations": {
		state tmp = node.rightmost;
		IList a1 = rewrite_class_member_declarations((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_selection_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "selection-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("selection-statement");

	case "selection-statement : if-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_if_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "selection-statement : switch-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_switch_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "selection-statement : typeswitch-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_typeswitch_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("expression");

	case "expression : conditional-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_conditional_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "expression : assignment": {
		state tmp = node.rightmost;
		expression a1 = rewrite_assignment((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_element_access(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "element-access");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("element-access");

	case "element-access : primary-expression [ expression-list ]": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a3 = rewrite_expression_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_primary_expression((nonterminalState)tmp);;

		retval = new element_access(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_argument_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "argument-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("argument-list");

	case "argument-list : argument": {
		state tmp = node.rightmost;
		argument a1 = rewrite_argument((nonterminalState)tmp);;

		retval = argumentList.New(a1);
		break;
		}

	case "argument-list : argument-list , argument": {
		state tmp = node.rightmost;
		argument a3 = rewrite_argument((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_argument_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static const_declarator rewrite_constant_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (const_declarator)disambiguate.resolve(node, "constant-declarator");
	}
	const_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("constant-declarator");

	case "constant-declarator : identifier = constant-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_constant_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new const_declarator(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_object_delegate_creation_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "object-delegate-creation-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("object-delegate-creation-expression");

	case "object-delegate-creation-expression : new type ( argument-listopt )": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_argument_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a2 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new new_expression(a2,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_member_access(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "member-access");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("member-access");

	case "member-access : primary-expression . identifier": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_primary_expression((nonterminalState)tmp);;

		retval = new expr_access(a1,a3);
		break;
		}

	case "member-access : predefined-type . identifier": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_predefined_type((nonterminalState)tmp);;

		retval = new predefined_access(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_constant_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "constant-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("constant-declaration");

	case "constant-declaration : attributesopt member-modifiersopt const type constant-declarators ;": {
		state tmp = node.rightmost;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a5 = rewrite_constant_declarators((nonterminalState)tmp);;
		tmp = tmp.below;
		type a4 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new constant_declaration(a1,a2,a4,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_constructor_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "constructor-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("constructor-declaration");

	case "constructor-declaration : attributesopt member-modifiersopt constructor-declarator constructor-body": {
		state tmp = node.rightmost;
		statement a4 = rewrite_constructor_body((nonterminalState)tmp);;
		tmp = tmp.below;
		constructor_declarator a3 = rewrite_constructor_declarator((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new constructor_declaration(a1,a2,a3,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_multiplicative_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "multiplicative-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("multiplicative-expression");

	case "multiplicative-expression : unary-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_unary_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "multiplicative-expression : multiplicative-expression * unary-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_multiplicative_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "multiplicative-expression : multiplicative-expression / unary-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_multiplicative_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}

	case "multiplicative-expression : multiplicative-expression % unary-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_multiplicative_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_operator_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "operator-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("operator-declaration");

	case "operator-declaration : attributesopt member-modifiersopt operator-declarator block": {
		state tmp = node.rightmost;
		statement a4 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		operator_declarator a3 = rewrite_operator_declarator((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new operator_declaration(a1,a2,a3,a4);
		break;
		}

	case "operator-declaration : attributesopt member-modifiersopt operator-declarator ;": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		operator_declarator a3 = rewrite_operator_declarator((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new operator_declaration(a1,a2,a3,null);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_conditional_or_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "conditional-or-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("conditional-or-expression");

	case "conditional-or-expression : conditional-and-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_conditional_and_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "conditional-or-expression : conditional-or-expression || conditional-and-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_conditional_and_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_conditional_or_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static event_accessor rewrite_add_accessor_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (event_accessor)disambiguate.resolve(node, "add-accessor-declaration");
	}
	event_accessor retval;
	switch (node.rule) {
	default: throw new System.Exception("add-accessor-declaration");

	case "add-accessor-declaration : attributesopt identifier===add block": {
		state tmp = node.rightmost;
		statement a3 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new event_accessor(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static compilation_unit rewrite_compilation_unit(nonterminalState node) {
	if (node.queue != null) {
		return (compilation_unit)disambiguate.resolve(node, "compilation-unit");
	}
	compilation_unit retval;
	switch (node.rule) {
	default: throw new System.Exception("compilation-unit");

	case "compilation-unit : using-directivesopt global-attributesopt namespace-member-declarationsopt": {
		state tmp = node.rightmost;
		IList a3 = rewrite_namespace_member_declarationsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_global_attributesopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_using_directivesopt((nonterminalState)tmp);;

		retval = new compilation_unit(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static attribute_section rewrite_global_attribute_section(nonterminalState node) {
	if (node.queue != null) {
		return (attribute_section)disambiguate.resolve(node, "global-attribute-section");
	}
	attribute_section retval;
	switch (node.rule) {
	default: throw new System.Exception("global-attribute-section");

	case "global-attribute-section : [ global-attribute-target : attribute-list ]": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_attribute_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_global_attribute_target((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new attribute_section(a2,a4);
		break;
		}

	case "global-attribute-section : [ global-attribute-target : attribute-list , ]": {
		state tmp = node.rightmost;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_attribute_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_global_attribute_target((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new attribute_section(a2,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_class_member_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "class-member-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("class-member-declaration");

	case "class-member-declaration : constant-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_constant_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : field-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_field_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : method-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_method_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : property-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_property_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : event-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_event_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : indexer-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_indexer_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : operator-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_operator_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : constructor-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_constructor_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : destructor-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_destructor_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "class-member-declaration : type-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_type_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_while_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "while-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("while-statement");

	case "while-statement : while ( boolean-expression ) embedded-statement": {
		state tmp = node.rightmost;
		statement a5 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_boolean_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new while_statement(a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_variable_declarators(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "variable-declarators");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("variable-declarators");

	case "variable-declarators : variable-declarator": {
		state tmp = node.rightmost;
		var_declarator a1 = rewrite_variable_declarator((nonterminalState)tmp);;

		retval = declaratorList.New(a1);
		break;
		}

	case "variable-declarators : variable-declarators , variable-declarator": {
		state tmp = node.rightmost;
		var_declarator a3 = rewrite_variable_declarator((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_variable_declarators((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static operator_declarator rewrite_unary_operator_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (operator_declarator)disambiguate.resolve(node, "unary-operator-declarator");
	}
	operator_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("unary-operator-declarator");

	case "unary-operator-declarator : type operator overloadable-unary-operator ( type identifier )": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a5 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_overloadable_unary_operator((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = new unary_declarator(a1,a3,a5,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_labeled_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "labeled-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("labeled-statement");

	case "labeled-statement : identifier : statement": {
		state tmp = node.rightmost;
		statement a3 = rewrite_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new labeled_statement(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_struct_member_declarationsopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "struct-member-declarationsopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("struct-member-declarationsopt");

	case "struct-member-declarationsopt :": {

		retval = declarationList.New();
		break;
		}

	case "struct-member-declarationsopt : struct-member-declarations": {
		state tmp = node.rightmost;
		IList a1 = rewrite_struct_member_declarations((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_argument_listopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "argument-listopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("argument-listopt");

	case "argument-listopt :": {

		retval = argumentList.New();
		break;
		}

	case "argument-listopt : argument-list": {
		state tmp = node.rightmost;
		IList a1 = rewrite_argument_list((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_class_body(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "class-body");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("class-body");

	case "class-body : { class-member-declarationsopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_class_member_declarationsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_expression_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "expression-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("expression-list");

	case "expression-list : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = expressionList.New(a1);
		break;
		}

	case "expression-list : expression-list , expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_expression_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_fixed_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "fixed-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("fixed-statement");

	case "fixed-statement : fixed ( pointer-type fixed-pointer-declarators ) embedded-statement": {
		state tmp = node.rightmost;
		statement a6 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_fixed_pointer_declarators((nonterminalState)tmp);;
		tmp = tmp.below;
		type a3 = rewrite_pointer_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new fixed_statement(a3,a4,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_interface_type_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "interface-type-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-type-list");

	case "interface-type-list : type-name": {
		state tmp = node.rightmost;
		type a1 = rewrite_type_name((nonterminalState)tmp);;

		retval = typeList.New(a1);
		break;
		}

	case "interface-type-list : interface-type-list , type-name": {
		state tmp = node.rightmost;
		type a3 = rewrite_type_name((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_interface_type_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static switch_section rewrite_switch_section(nonterminalState node) {
	if (node.queue != null) {
		return (switch_section)disambiguate.resolve(node, "switch-section");
	}
	switch_section retval;
	switch (node.rule) {
	default: throw new System.Exception("switch-section");

	case "switch-section : switch-labels statement-list": {
		state tmp = node.rightmost;
		IList a2 = rewrite_statement_list((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_switch_labels((nonterminalState)tmp);;

		retval = new switch_section(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static using_directive rewrite_using_alias_directive(nonterminalState node) {
	if (node.queue != null) {
		return (using_directive)disambiguate.resolve(node, "using-alias-directive");
	}
	using_directive retval;
	switch (node.rule) {
	default: throw new System.Exception("using-alias-directive");

	case "using-alias-directive : using identifier = namespace-or-type-name ;": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		dotted_name a4 = rewrite_namespace_or_type_name((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new alias_directive(a2,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_switch_sections(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "switch-sections");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("switch-sections");

	case "switch-sections : switch-section": {
		state tmp = node.rightmost;
		switch_section a1 = rewrite_switch_section((nonterminalState)tmp);;

		retval = switch_sectionList.New(a1);
		break;
		}

	case "switch-sections : switch-sections switch-section": {
		state tmp = node.rightmost;
		switch_section a2 = rewrite_switch_section((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_switch_sections((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_if_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "if-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("if-statement");

	case "if-statement : if ( boolean-expression ) embedded-statement": {
		state tmp = node.rightmost;
		statement a5 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_boolean_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new if_statement(a3,a5,null);
		break;
		}

	case "if-statement : if ( boolean-expression ) embedded-statement else embedded-statement": {
		state tmp = node.rightmost;
		statement a7 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		statement a5 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_boolean_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new if_statement(a3,a5,a7);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static for_init rewrite_for_initializer(nonterminalState node) {
	if (node.queue != null) {
		return (for_init)disambiguate.resolve(node, "for-initializer");
	}
	for_init retval;
	switch (node.rule) {
	default: throw new System.Exception("for-initializer");

	case "for-initializer : local-variable-declaration": {
		state tmp = node.rightmost;
		statement a1 = rewrite_local_variable_declaration((nonterminalState)tmp);;

		retval = new for_decl((local_statement)a1);
		break;
		}

	case "for-initializer : statement-expression-list": {
		state tmp = node.rightmost;
		IList a1 = rewrite_statement_expression_list((nonterminalState)tmp);;

		retval = new for_list(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_checked_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "checked-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("checked-statement");

	case "checked-statement : checked block": {
		state tmp = node.rightmost;
		statement a2 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new checked_statement(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_global_attributes(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "global-attributes");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("global-attributes");

	case "global-attributes : global-attribute-sections": {
		state tmp = node.rightmost;
		IList a1 = rewrite_global_attribute_sections((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_enum_member_declarations(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "enum-member-declarations");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("enum-member-declarations");

	case "enum-member-declarations : enum-member-declaration": {
		state tmp = node.rightmost;
		enum_member_declaration a1 = rewrite_enum_member_declaration((nonterminalState)tmp);;

		retval = enum_member_declarationList.New(a1);
		break;
		}

	case "enum-member-declarations : enum-member-declarations , enum-member-declaration": {
		state tmp = node.rightmost;
		enum_member_declaration a3 = rewrite_enum_member_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_enum_member_declarations((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_type_name(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "type-name");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("type-name");

	case "type-name : namespace-or-type-name": {
		state tmp = node.rightmost;
		dotted_name a1 = rewrite_namespace_or_type_name((nonterminalState)tmp);;

		retval = new name_type(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_overloadable_unary_operator(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "overloadable-unary-operator");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("overloadable-unary-operator");

	case "overloadable-unary-operator : +": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-unary-operator : -": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-unary-operator : !": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-unary-operator : ~": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-unary-operator : ++": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-unary-operator : --": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-unary-operator : true": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-unary-operator : false": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_switch_block(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "switch-block");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("switch-block");

	case "switch-block : { switch-sectionsopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_switch_sectionsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_empty_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "empty-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("empty-statement");

	case "empty-statement : ;": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new empty_statement();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_break_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "break-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("break-statement");

	case "break-statement : break ;": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new break_statement();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_identifieropt(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "identifieropt");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("identifieropt");

	case "identifieropt :": {

		retval = null;
		break;
		}

	case "identifieropt : identifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_for_condition(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "for-condition");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("for-condition");

	case "for-condition : boolean-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_boolean_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_overloadable_binary_operator(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "overloadable-binary-operator");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("overloadable-binary-operator");

	case "overloadable-binary-operator : +": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : -": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : *": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : /": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : %": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : &": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : |": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : ^": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : <<": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : >>": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : ==": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : !=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : >": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : <": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : >=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "overloadable-binary-operator : <=": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_addressof_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "addressof-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("addressof-expression");

	case "addressof-expression : & unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unary_expression(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static named_argument rewrite_named_argument(nonterminalState node) {
	if (node.queue != null) {
		return (named_argument)disambiguate.resolve(node, "named-argument");
	}
	named_argument retval;
	switch (node.rule) {
	default: throw new System.Exception("named-argument");

	case "named-argument : identifier = attribute-argument-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_attribute_argument_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new named_argument(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_class_type_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "class-type-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("class-type-list");

	case "class-type-list : class-type": {
		state tmp = node.rightmost;
		type a1 = rewrite_class_type((nonterminalState)tmp);;

		retval = typeList.New(a1);
		break;
		}

	case "class-type-list : class-type-list , class-type": {
		state tmp = node.rightmost;
		type a3 = rewrite_class_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_class_type_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_post_increment_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "post-increment-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("post-increment-expression");

	case "post-increment-expression : primary-expression ++": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_primary_expression((nonterminalState)tmp);;

		retval = new post_expression(a2,a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_namespace_member_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "namespace-member-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("namespace-member-declaration");

	case "namespace-member-declaration : namespace-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_namespace_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "namespace-member-declaration : type-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_type_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static array_initializer rewrite_array_initializeropt(nonterminalState node) {
	if (node.queue != null) {
		return (array_initializer)disambiguate.resolve(node, "array-initializeropt");
	}
	array_initializer retval;
	switch (node.rule) {
	default: throw new System.Exception("array-initializeropt");

	case "array-initializeropt :": {

		retval = null;
		break;
		}

	case "array-initializeropt : array-initializer": {
		state tmp = node.rightmost;
		array_initializer a1 = rewrite_array_initializer((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static member_name rewrite_member_name(nonterminalState node) {
	if (node.queue != null) {
		return (member_name)disambiguate.resolve(node, "member-name");
	}
	member_name retval;
	switch (node.rule) {
	default: throw new System.Exception("member-name");

	case "member-name : identifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new member_name(null,a1);
		break;
		}

	case "member-name : type-name . identifier": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a1 = rewrite_type_name((nonterminalState)tmp);;

		retval = new member_name(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static attribute_section rewrite_attribute_section(nonterminalState node) {
	if (node.queue != null) {
		return (attribute_section)disambiguate.resolve(node, "attribute-section");
	}
	attribute_section retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-section");

	case "attribute-section : [ attribute-target-specifieropt attribute-list ]": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a3 = rewrite_attribute_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_attribute_target_specifieropt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new attribute_section(a2,a3);
		break;
		}

	case "attribute-section : [ attribute-target-specifieropt attribute-list , ]": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a3 = rewrite_attribute_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_attribute_target_specifieropt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new attribute_section(a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static argument rewrite_argument(nonterminalState node) {
	if (node.queue != null) {
		return (argument)disambiguate.resolve(node, "argument");
	}
	argument retval;
	switch (node.rule) {
	default: throw new System.Exception("argument");

	case "argument : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = new argument(null, a1);
		break;
		}

	case "argument : ref variable-reference": {
		state tmp = node.rightmost;
		expression a2 = rewrite_variable_reference((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new argument(a1, a2);
		break;
		}

	case "argument : out variable-reference": {
		state tmp = node.rightmost;
		expression a2 = rewrite_variable_reference((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new argument(a1, a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("statement");

	case "statement : labeled-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_labeled_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "statement : declaration-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_declaration_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "statement : embedded-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_embedded_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static fixed_declarator rewrite_fixed_pointer_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (fixed_declarator)disambiguate.resolve(node, "fixed-pointer-declarator");
	}
	fixed_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("fixed-pointer-declarator");

	case "fixed-pointer-declarator : identifier = expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new fixed_declarator(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_embedded_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "embedded-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("embedded-statement");

	case "embedded-statement : block": {
		state tmp = node.rightmost;
		statement a1 = rewrite_block((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : empty-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_empty_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : expression-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_expression_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : selection-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_selection_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : iteration-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_iteration_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : jump-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_jump_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : try-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_try_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : checked-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_checked_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : unchecked-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_unchecked_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : lock-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_lock_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : using-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_using_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "embedded-statement : fixed-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_fixed_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_lock_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "lock-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("lock-statement");

	case "lock-statement : lock ( expression ) embedded-statement": {
		state tmp = node.rightmost;
		statement a5 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new lock_statement(a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static accessor_declaration rewrite_set_accessor_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (accessor_declaration)disambiguate.resolve(node, "set-accessor-declaration");
	}
	accessor_declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("set-accessor-declaration");

	case "set-accessor-declaration : attributesopt identifier===set accessor-body": {
		state tmp = node.rightmost;
		statement a3 = rewrite_accessor_body((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new accessor_declaration(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static catch_clause rewrite_specific_catch_clause(nonterminalState node) {
	if (node.queue != null) {
		return (catch_clause)disambiguate.resolve(node, "specific-catch-clause");
	}
	catch_clause retval;
	switch (node.rule) {
	default: throw new System.Exception("specific-catch-clause");

	case "specific-catch-clause : catch ( class-type identifieropt ) block": {
		state tmp = node.rightmost;
		statement a6 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_identifieropt((nonterminalState)tmp);;
		tmp = tmp.below;
		type a3 = rewrite_class_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new catch_clause(a3,a4,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static event_accessor rewrite_remove_accessor_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (event_accessor)disambiguate.resolve(node, "remove-accessor-declaration");
	}
	event_accessor retval;
	switch (node.rule) {
	default: throw new System.Exception("remove-accessor-declaration");

	case "remove-accessor-declaration : attributesopt identifier===remove block": {
		state tmp = node.rightmost;
		statement a3 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new event_accessor(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_and_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "and-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("and-expression");

	case "and-expression : equality-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_equality_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "and-expression : and-expression & equality-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_equality_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_and_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static parameter rewrite_parameter_array(nonterminalState node) {
	if (node.queue != null) {
		return (parameter)disambiguate.resolve(node, "parameter-array");
	}
	parameter retval;
	switch (node.rule) {
	default: throw new System.Exception("parameter-array");

	case "parameter-array : attributesopt params array-type identifier": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_array_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new params_parameter(a1,a3,a4);
		break;
		}

	case "parameter-array : identifier===__arglist": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new arglist_parameter(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static fixed_parameter rewrite_fixed_parameter(nonterminalState node) {
	if (node.queue != null) {
		return (fixed_parameter)disambiguate.resolve(node, "fixed-parameter");
	}
	fixed_parameter retval;
	switch (node.rule) {
	default: throw new System.Exception("fixed-parameter");

	case "fixed-parameter : attributesopt parameter-modifieropt type identifier": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_parameter_modifieropt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new fixed_parameter(a1,a2,a3,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_interface_member_declarations(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "interface-member-declarations");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-member-declarations");

	case "interface-member-declarations : interface-member-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_interface_member_declaration((nonterminalState)tmp);;

		retval = declarationList.New(a1);
		break;
		}

	case "interface-member-declarations : interface-member-declarations interface-member-declaration": {
		state tmp = node.rightmost;
		declaration a2 = rewrite_interface_member_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_interface_member_declarations((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_using_directives(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "using-directives");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("using-directives");

	case "using-directives : using-directive": {
		state tmp = node.rightmost;
		using_directive a1 = rewrite_using_directive((nonterminalState)tmp);;

		retval = using_directiveList.New(a1);
		break;
		}

	case "using-directives : using-directives using-directive": {
		state tmp = node.rightmost;
		using_directive a2 = rewrite_using_directive((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_using_directives((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_newopt(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "newopt");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("newopt");

	case "newopt :": {

		retval = null;
		break;
		}

	case "newopt : new": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_throw_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "throw-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("throw-statement");

	case "throw-statement : throw expressionopt ;": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a2 = rewrite_expressionopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new throw_statement(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_struct_member_declarations(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "struct-member-declarations");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("struct-member-declarations");

	case "struct-member-declarations : struct-member-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_struct_member_declaration((nonterminalState)tmp);;

		retval = declarationList.New(a1);
		break;
		}

	case "struct-member-declarations : struct-member-declarations struct-member-declaration": {
		state tmp = node.rightmost;
		declaration a2 = rewrite_struct_member_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_struct_member_declarations((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_pointer_member_access(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "pointer-member-access");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("pointer-member-access");

	case "pointer-member-access : primary-expression -> identifier": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_primary_expression((nonterminalState)tmp);;

		retval = new pointer_access(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static attribute rewrite_attribute(nonterminalState node) {
	if (node.queue != null) {
		return (attribute)disambiguate.resolve(node, "attribute");
	}
	attribute retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute");

	case "attribute : attribute-name attribute-argumentsopt": {
		state tmp = node.rightmost;
		attribute_arguments a2 = rewrite_attribute_argumentsopt((nonterminalState)tmp);;
		tmp = tmp.below;
		type a1 = rewrite_attribute_name((nonterminalState)tmp);;

		retval = new attribute(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_using_directivesopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "using-directivesopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("using-directivesopt");

	case "using-directivesopt :": {

		retval = using_directiveList.New();
		break;
		}

	case "using-directivesopt : using-directives": {
		state tmp = node.rightmost;
		IList a1 = rewrite_using_directives((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_constant_declarators(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "constant-declarators");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("constant-declarators");

	case "constant-declarators : constant-declarator": {
		state tmp = node.rightmost;
		const_declarator a1 = rewrite_constant_declarator((nonterminalState)tmp);;

		retval = declaratorList.New(a1);
		break;
		}

	case "constant-declarators : constant-declarators , constant-declarator": {
		state tmp = node.rightmost;
		const_declarator a3 = rewrite_constant_declarator((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_constant_declarators((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_specific_catch_clausesopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "specific-catch-clausesopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("specific-catch-clausesopt");

	case "specific-catch-clausesopt :": {

		retval = catch_clauseList.New();
		break;
		}

	case "specific-catch-clausesopt : specific-catch-clauses": {
		state tmp = node.rightmost;
		IList a1 = rewrite_specific_catch_clauses((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static array_initializer rewrite_array_initializer(nonterminalState node) {
	if (node.queue != null) {
		return (array_initializer)disambiguate.resolve(node, "array-initializer");
	}
	array_initializer retval;
	switch (node.rule) {
	default: throw new System.Exception("array-initializer");

	case "array-initializer : { variable-initializer-listopt }": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_variable_initializer_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new array_initializer(a2);
		break;
		}

	case "array-initializer : { variable-initializer-list , }": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_variable_initializer_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new array_initializer(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static indexer rewrite_indexer_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (indexer)disambiguate.resolve(node, "indexer-declarator");
	}
	indexer retval;
	switch (node.rule) {
	default: throw new System.Exception("indexer-declarator");

	case "indexer-declarator : type this [ formal-parameter-list ]": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		formals a4 = rewrite_formal_parameter_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = new indexer(a1,null,a4);
		break;
		}

	case "indexer-declarator : type type-name . this [ formal-parameter-list ]": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		formals a6 = rewrite_formal_parameter_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a2 = rewrite_type_name((nonterminalState)tmp);;
		tmp = tmp.below;
		type a1 = rewrite_type((nonterminalState)tmp);;

		retval = new indexer(a1,(name_type)a2,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_named_argument_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "named-argument-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("named-argument-list");

	case "named-argument-list : named-argument": {
		state tmp = node.rightmost;
		named_argument a1 = rewrite_named_argument((nonterminalState)tmp);;

		retval = named_argumentList.New(a1);
		break;
		}

	case "named-argument-list : named-argument-list , named-argument": {
		state tmp = node.rightmost;
		named_argument a3 = rewrite_named_argument((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_named_argument_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_exclusive_or_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "exclusive-or-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("exclusive-or-expression");

	case "exclusive-or-expression : and-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_and_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "exclusive-or-expression : exclusive-or-expression ^ and-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_and_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_exclusive_or_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_variable_initializer_listopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "variable-initializer-listopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("variable-initializer-listopt");

	case "variable-initializer-listopt :": {

		retval = variable_initializerList.New();
		break;
		}

	case "variable-initializer-listopt : variable-initializer-list": {
		state tmp = node.rightmost;
		IList a1 = rewrite_variable_initializer_list((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_try_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "try-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("try-statement");

	case "try-statement : try block catch-clauses": {
		state tmp = node.rightmost;
		catch_clauses a3 = rewrite_catch_clauses((nonterminalState)tmp);;
		tmp = tmp.below;
		statement a2 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new try_statement(a2,a3,null);
		break;
		}

	case "try-statement : try block catch-clausesopt finally-clause": {
		state tmp = node.rightmost;
		finally_clause a4 = rewrite_finally_clause((nonterminalState)tmp);;
		tmp = tmp.below;
		catch_clauses a3 = rewrite_catch_clausesopt((nonterminalState)tmp);;
		tmp = tmp.below;
		statement a2 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new try_statement(a2,a3,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_foreach_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "foreach-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("foreach-statement");

	case "foreach-statement : foreach ( type identifier in expression ) embedded-statement": {
		state tmp = node.rightmost;
		statement a8 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a6 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new foreach_statement(a3,a4,a6,a8);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_member_modifier(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "member-modifier");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("member-modifier");

	case "member-modifier : abstract": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : extern": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : internal": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : new": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : override": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : private": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : protected": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : public": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : readonly": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : sealed": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : static": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : unsafe": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : virtual": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "member-modifier : volatile": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_typeswitch_labels(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "typeswitch-labels");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("typeswitch-labels");

	case "typeswitch-labels : typeswitch-label": {
		state tmp = node.rightmost;
		switch_label a1 = rewrite_typeswitch_label((nonterminalState)tmp);;

		retval = switch_labelList.New(a1);
		break;
		}

	case "typeswitch-labels : typeswitch-labels typeswitch-label": {
		state tmp = node.rightmost;
		switch_label a2 = rewrite_typeswitch_label((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_typeswitch_labels((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_positional_argument_listopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "positional-argument-listopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("positional-argument-listopt");

	case "positional-argument-listopt :": {

		retval = expressionList.New();
		break;
		}

	case "positional-argument-listopt : positional-argument-list": {
		state tmp = node.rightmost;
		IList a1 = rewrite_positional_argument_list((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_type(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "type");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("type");

	case "type : bool": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new bool_type();
		break;
		}

	case "type : integral-type": {
		state tmp = node.rightmost;
		type a1 = rewrite_integral_type((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "type : decimal": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new decimal_type();
		break;
		}

	case "type : float": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new float_type();
		break;
		}

	case "type : double": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new double_type();
		break;
		}

	case "type : class-type": {
		state tmp = node.rightmost;
		type a1 = rewrite_class_type((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "type : array-type": {
		state tmp = node.rightmost;
		type a1 = rewrite_array_type((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "type : pointer-type": {
		state tmp = node.rightmost;
		type a1 = rewrite_pointer_type((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_parenthesized_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "parenthesized-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("parenthesized-expression");

	case "parenthesized-expression : ( expression )": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a2 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_specific_catch_clauses(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "specific-catch-clauses");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("specific-catch-clauses");

	case "specific-catch-clauses : specific-catch-clause": {
		state tmp = node.rightmost;
		catch_clause a1 = rewrite_specific_catch_clause((nonterminalState)tmp);;

		retval = catch_clauseList.New(a1);
		break;
		}

	case "specific-catch-clauses : specific-catch-clauses specific-catch-clause": {
		state tmp = node.rightmost;
		catch_clause a2 = rewrite_specific_catch_clause((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_specific_catch_clauses((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static variable_initializer rewrite_stackalloc_initializer(nonterminalState node) {
	if (node.queue != null) {
		return (variable_initializer)disambiguate.resolve(node, "stackalloc-initializer");
	}
	variable_initializer retval;
	switch (node.rule) {
	default: throw new System.Exception("stackalloc-initializer");

	case "stackalloc-initializer : stackalloc unmanaged-type [ expression ]": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a4 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a2 = rewrite_unmanaged_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new stackalloc_initializer(a2,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_pointer_type(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "pointer-type");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("pointer-type");

	case "pointer-type : unmanaged-type *": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a1 = rewrite_unmanaged_type((nonterminalState)tmp);;

		retval = new pointer_type(a1);
		break;
		}

	case "pointer-type : void *": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new void_pointer_type();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_parameter_modifier(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "parameter-modifier");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("parameter-modifier");

	case "parameter-modifier : ref": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "parameter-modifier : out": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_assignment(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "assignment");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("assignment");

	case "assignment : unary-expression = expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_unary_expression((nonterminalState)tmp);;

		retval = new assignment_expression(a1,a3);
		break;
		}

	case "assignment : unary-expression assignment-operator expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_assignment_operator((nonterminalState)tmp);;
		tmp = tmp.below;
		expression a1 = rewrite_unary_expression((nonterminalState)tmp);;

		retval = new compound_assignment_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_unchecked_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "unchecked-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("unchecked-statement");

	case "unchecked-statement : unchecked block": {
		state tmp = node.rightmost;
		statement a2 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unchecked_statement(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_conditional_and_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "conditional-and-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("conditional-and-expression");

	case "conditional-and-expression : inclusive-or-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_inclusive_or_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "conditional-and-expression : conditional-and-expression && inclusive-or-expression": {
		state tmp = node.rightmost;
		expression a3 = rewrite_inclusive_or_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_conditional_and_expression((nonterminalState)tmp);;

		retval = new binary_expression(a1,a2,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_method_body(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "method-body");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("method-body");

	case "method-body : block": {
		state tmp = node.rightmost;
		statement a1 = rewrite_block((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "method-body : ;": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = null;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static method_header rewrite_method_header(nonterminalState node) {
	if (node.queue != null) {
		return (method_header)disambiguate.resolve(node, "method-header");
	}
	method_header retval;
	switch (node.rule) {
	default: throw new System.Exception("method-header");

	case "method-header : attributesopt member-modifiersopt return-type member-name ( formal-parameter-listopt )": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		formals a6 = rewrite_formal_parameter_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		member_name a4 = rewrite_member_name((nonterminalState)tmp);;
		tmp = tmp.below;
		type a3 = rewrite_return_type((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new method_header(a1,a2,a3,a4,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_parameter_modifieropt(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "parameter-modifieropt");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("parameter-modifieropt");

	case "parameter-modifieropt :": {

		retval = null;
		break;
		}

	case "parameter-modifieropt : parameter-modifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_parameter_modifier((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static catch_clauses rewrite_catch_clausesopt(nonterminalState node) {
	if (node.queue != null) {
		return (catch_clauses)disambiguate.resolve(node, "catch-clausesopt");
	}
	catch_clauses retval;
	switch (node.rule) {
	default: throw new System.Exception("catch-clausesopt");

	case "catch-clausesopt :": {

		retval = null;
		break;
		}

	case "catch-clausesopt : catch-clauses": {
		state tmp = node.rightmost;
		catch_clauses a1 = rewrite_catch_clauses((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_sizeof_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "sizeof-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("sizeof-expression");

	case "sizeof-expression : sizeof ( unmanaged-type )": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_unmanaged_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new sizeof_expression(a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static enum_member_declaration rewrite_enum_member_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (enum_member_declaration)disambiguate.resolve(node, "enum-member-declaration");
	}
	enum_member_declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("enum-member-declaration");

	case "enum-member-declaration : attributesopt identifier": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new enum_member_declaration(a1,a2,null);
		break;
		}

	case "enum-member-declaration : attributesopt identifier = constant-expression": {
		state tmp = node.rightmost;
		expression a4 = rewrite_constant_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new enum_member_declaration(a1,a2,a4);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_do_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "do-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("do-statement");

	case "do-statement : do embedded-statement while ( boolean-expression ) ;": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a5 = rewrite_boolean_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		statement a2 = rewrite_embedded_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new do_statement(a2,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_statement_listopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "statement-listopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("statement-listopt");

	case "statement-listopt :": {

		retval = statementList.New();
		break;
		}

	case "statement-listopt : statement-list": {
		state tmp = node.rightmost;
		IList a1 = rewrite_statement_list((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_for_iteratoropt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "for-iteratoropt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("for-iteratoropt");

	case "for-iteratoropt :": {

		retval = expressionList.New();
		break;
		}

	case "for-iteratoropt : for-iterator": {
		state tmp = node.rightmost;
		IList a1 = rewrite_for_iterator((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_attribute_argument_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "attribute-argument-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-argument-expression");

	case "attribute-argument-expression : conditional-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_conditional_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_expressionopt(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "expressionopt");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("expressionopt");

	case "expressionopt :": {

		retval = null;
		break;
		}

	case "expressionopt : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static dotted_name rewrite_namespace_or_type_name(nonterminalState node) {
	if (node.queue != null) {
		return (dotted_name)disambiguate.resolve(node, "namespace-or-type-name");
	}
	dotted_name retval;
	switch (node.rule) {
	default: throw new System.Exception("namespace-or-type-name");

	case "namespace-or-type-name : identifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new dotted_name(null,a1);
		break;
		}

	case "namespace-or-type-name : namespace-or-type-name . identifier": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		dotted_name a1 = rewrite_namespace_or_type_name((nonterminalState)tmp);;

		retval = new dotted_name(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_checked_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "checked-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("checked-expression");

	case "checked-expression : checked ( expression )": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new checked_expression(a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_boolean_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "boolean-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("boolean-expression");

	case "boolean-expression : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_typeof_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "typeof-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("typeof-expression");

	case "typeof-expression : typeof ( return-type )": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_return_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new typeof_expression(a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_switch_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "switch-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("switch-statement");

	case "switch-statement : switch ( expression ) switch-block": {
		state tmp = node.rightmost;
		IList a5 = rewrite_switch_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new switch_statement(a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_declaration_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "declaration-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("declaration-statement");

	case "declaration-statement : local-variable-declaration ;": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		statement a1 = rewrite_local_variable_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "declaration-statement : local-constant-declaration ;": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		statement a1 = rewrite_local_constant_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static compilation_unit rewrite_START(nonterminalState node) {
	if (node.queue != null) {
		return (compilation_unit)disambiguate.resolve(node, "START");
	}
	compilation_unit retval;
	switch (node.rule) {
	default: throw new System.Exception("START");

	case "START : compilation-unit": {
		state tmp = node.rightmost;
		compilation_unit a1 = rewrite_compilation_unit((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_general_catch_clause(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "general-catch-clause");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("general-catch-clause");

	case "general-catch-clause : catch block": {
		state tmp = node.rightmost;
		statement a2 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a2;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_global_attribute_sections(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "global-attribute-sections");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("global-attribute-sections");

	case "global-attribute-sections : global-attribute-section": {
		state tmp = node.rightmost;
		attribute_section a1 = rewrite_global_attribute_section((nonterminalState)tmp);;

		retval = attribute_sectionList.New(a1);
		break;
		}

	case "global-attribute-sections : global-attribute-sections global-attribute-section": {
		state tmp = node.rightmost;
		attribute_section a2 = rewrite_global_attribute_section((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_global_attribute_sections((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_return_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "return-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("return-statement");

	case "return-statement : return expressionopt ;": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a2 = rewrite_expressionopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new return_statement(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_class_member_declarations(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "class-member-declarations");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("class-member-declarations");

	case "class-member-declarations : class-member-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_class_member_declaration((nonterminalState)tmp);;

		retval = declarationList.New(a1);
		break;
		}

	case "class-member-declarations : class-member-declarations class-member-declaration": {
		state tmp = node.rightmost;
		declaration a2 = rewrite_class_member_declaration((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_class_member_declarations((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static catch_clauses rewrite_catch_clauses(nonterminalState node) {
	if (node.queue != null) {
		return (catch_clauses)disambiguate.resolve(node, "catch-clauses");
	}
	catch_clauses retval;
	switch (node.rule) {
	default: throw new System.Exception("catch-clauses");

	case "catch-clauses : specific-catch-clauses": {
		state tmp = node.rightmost;
		IList a1 = rewrite_specific_catch_clauses((nonterminalState)tmp);;

		retval = new catch_clauses(a1,null);
		break;
		}

	case "catch-clauses : specific-catch-clausesopt general-catch-clause": {
		state tmp = node.rightmost;
		statement a2 = rewrite_general_catch_clause((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_specific_catch_clausesopt((nonterminalState)tmp);;

		retval = new catch_clauses(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_interface_baseopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "interface-baseopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-baseopt");

	case "interface-baseopt :": {

		retval = typeList.New();
		break;
		}

	case "interface-baseopt : interface-base": {
		state tmp = node.rightmost;
		IList a1 = rewrite_interface_base((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_typeswitch_sectionsopt(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "typeswitch-sectionsopt");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("typeswitch-sectionsopt");

	case "typeswitch-sectionsopt :": {

		retval = typeswitch_sectionList.New();
		break;
		}

	case "typeswitch-sectionsopt : typeswitch-sections": {
		state tmp = node.rightmost;
		IList a1 = rewrite_typeswitch_sections((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_destructor_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "destructor-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("destructor-declaration");

	case "destructor-declaration : attributesopt member-modifiersopt ~ identifier ( ) block": {
		state tmp = node.rightmost;
		statement a7 = rewrite_block((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new destructor_declaration(a1,a2,a4,a7);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_constant_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "constant-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("constant-expression");

	case "constant-expression : expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static attribute_arguments rewrite_attribute_argumentsopt(nonterminalState node) {
	if (node.queue != null) {
		return (attribute_arguments)disambiguate.resolve(node, "attribute-argumentsopt");
	}
	attribute_arguments retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-argumentsopt");

	case "attribute-argumentsopt :": {

		retval = null;
		break;
		}

	case "attribute-argumentsopt : attribute-arguments": {
		state tmp = node.rightmost;
		attribute_arguments a1 = rewrite_attribute_arguments((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_boolean_literal(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "boolean-literal");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("boolean-literal");

	case "boolean-literal : true": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new boolean_literal(a1);
		break;
		}

	case "boolean-literal : false": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new boolean_literal(a1);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static using_directive rewrite_using_namespace_directive(nonterminalState node) {
	if (node.queue != null) {
		return (using_directive)disambiguate.resolve(node, "using-namespace-directive");
	}
	using_directive retval;
	switch (node.rule) {
	default: throw new System.Exception("using-namespace-directive");

	case "using-namespace-directive : using namespace-name ;": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		dotted_name a2 = rewrite_namespace_name((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new namespace_directive(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_variable_initializer_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "variable-initializer-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("variable-initializer-list");

	case "variable-initializer-list : variable-initializer": {
		state tmp = node.rightmost;
		variable_initializer a1 = rewrite_variable_initializer((nonterminalState)tmp);;

		retval = variable_initializerList.New(a1);
		break;
		}

	case "variable-initializer-list : variable-initializer-list , variable-initializer": {
		state tmp = node.rightmost;
		variable_initializer a3 = rewrite_variable_initializer((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_variable_initializer_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_unchecked_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "unchecked-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("unchecked-expression");

	case "unchecked-expression : unchecked ( expression )": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new unchecked_expression(a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_member_modifiers(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "member-modifiers");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("member-modifiers");

	case "member-modifiers : member-modifier": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_member_modifier((nonterminalState)tmp);;

		retval = InputElementList.New(a1);
		break;
		}

	case "member-modifiers : member-modifiers member-modifier": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_member_modifier((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_member_modifiers((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_statement_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "statement-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("statement-expression");

	case "statement-expression : invocation-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_invocation_expression((nonterminalState)tmp);;

		retval = a1.annotate(false);
		break;
		}

	case "statement-expression : object-delegate-creation-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_object_delegate_creation_expression((nonterminalState)tmp);;

		retval = a1.annotate(false);
		break;
		}

	case "statement-expression : assignment": {
		state tmp = node.rightmost;
		expression a1 = rewrite_assignment((nonterminalState)tmp);;

		retval = a1.annotate(false);
		break;
		}

	case "statement-expression : post-increment-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_post_increment_expression((nonterminalState)tmp);;

		retval = a1.annotate(false);
		break;
		}

	case "statement-expression : post-decrement-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_post_decrement_expression((nonterminalState)tmp);;

		retval = a1.annotate(false);
		break;
		}

	case "statement-expression : pre-increment-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_pre_increment_expression((nonterminalState)tmp);;

		retval = a1.annotate(false);
		break;
		}

	case "statement-expression : pre-decrement-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_pre_decrement_expression((nonterminalState)tmp);;

		retval = a1.annotate(false);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_primary_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "primary-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("primary-expression");

	case "primary-expression : array-creation-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_array_creation_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression : primary-expression-no-array-creation": {
		state tmp = node.rightmost;
		expression a1 = rewrite_primary_expression_no_array_creation((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "primary-expression : pointer-member-access": {
		state tmp = node.rightmost;
		expression a1 = rewrite_pointer_member_access((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static attribute_arguments rewrite_attribute_arguments(nonterminalState node) {
	if (node.queue != null) {
		return (attribute_arguments)disambiguate.resolve(node, "attribute-arguments");
	}
	attribute_arguments retval;
	switch (node.rule) {
	default: throw new System.Exception("attribute-arguments");

	case "attribute-arguments : ( positional-argument-listopt )": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_positional_argument_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new attribute_arguments(a2,named_argumentList.New());
		break;
		}

	case "attribute-arguments : ( positional-argument-list , named-argument-list )": {
		state tmp = node.rightmost;
		InputElement a5 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a4 = rewrite_named_argument_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_positional_argument_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new attribute_arguments(a2,a4);
		break;
		}

	case "attribute-arguments : ( named-argument-list )": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_named_argument_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new attribute_arguments(expressionList.New(),a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_struct_member_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "struct-member-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("struct-member-declaration");

	case "struct-member-declaration : constant-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_constant_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : field-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_field_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : method-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_method_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : property-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_property_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : event-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_event_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : indexer-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_indexer_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : operator-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_operator_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : constructor-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_constructor_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "struct-member-declaration : type-declaration": {
		state tmp = node.rightmost;
		declaration a1 = rewrite_type_declaration((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static operator_declarator rewrite_operator_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (operator_declarator)disambiguate.resolve(node, "operator-declarator");
	}
	operator_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("operator-declarator");

	case "operator-declarator : unary-operator-declarator": {
		state tmp = node.rightmost;
		operator_declarator a1 = rewrite_unary_operator_declarator((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "operator-declarator : binary-operator-declarator": {
		state tmp = node.rightmost;
		operator_declarator a1 = rewrite_binary_operator_declarator((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "operator-declarator : conversion-operator-declarator": {
		state tmp = node.rightmost;
		operator_declarator a1 = rewrite_conversion_operator_declarator((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_predefined_type(nonterminalState node) {
	if (node.queue != null) {
		return (InputElement)disambiguate.resolve(node, "predefined-type");
	}
	InputElement retval;
	switch (node.rule) {
	default: throw new System.Exception("predefined-type");

	case "predefined-type : bool": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : byte": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : char": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : decimal": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : double": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : float": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : int": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : long": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : object": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : sbyte": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : short": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : string": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : uint": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : ulong": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}

	case "predefined-type : ushort": {
		state tmp = node.rightmost;
		InputElement a1 = rewrite_terminal(tmp);

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_new_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "new-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("new-expression");

	case "new-expression : object-delegate-creation-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_object_delegate_creation_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_base_access(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "base-access");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("base-access");

	case "base-access : base . identifier": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new expr_access(new base_access(),a3);
		break;
		}

	case "base-access : base [ expression-list ]": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a3 = rewrite_expression_list((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new element_access(new base_access(),a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_positional_argument_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "positional-argument-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("positional-argument-list");

	case "positional-argument-list : positional-argument": {
		state tmp = node.rightmost;
		expression a1 = rewrite_positional_argument((nonterminalState)tmp);;

		retval = expressionList.New(a1);
		break;
		}

	case "positional-argument-list : positional-argument-list , positional-argument": {
		state tmp = node.rightmost;
		expression a3 = rewrite_positional_argument((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a1 = rewrite_positional_argument_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_statement_list(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "statement-list");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("statement-list");

	case "statement-list : statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_statement((nonterminalState)tmp);;

		retval = statementList.New(a1);
		break;
		}

	case "statement-list : statement-list statement": {
		state tmp = node.rightmost;
		statement a2 = rewrite_statement((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_statement_list((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static operator_declarator rewrite_conversion_operator_declarator(nonterminalState node) {
	if (node.queue != null) {
		return (operator_declarator)disambiguate.resolve(node, "conversion-operator-declarator");
	}
	operator_declarator retval;
	switch (node.rule) {
	default: throw new System.Exception("conversion-operator-declarator");

	case "conversion-operator-declarator : implicit operator type ( type identifier )": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a5 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new implicit_declarator(a3,a5,a6);
		break;
		}

	case "conversion-operator-declarator : explicit operator type ( type identifier )": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a5 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a3 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new explicit_declarator(a3,a5,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static constructor_initializer rewrite_constructor_initializeropt(nonterminalState node) {
	if (node.queue != null) {
		return (constructor_initializer)disambiguate.resolve(node, "constructor-initializeropt");
	}
	constructor_initializer retval;
	switch (node.rule) {
	default: throw new System.Exception("constructor-initializeropt");

	case "constructor-initializeropt :": {

		retval = null;
		break;
		}

	case "constructor-initializeropt : constructor-initializer": {
		state tmp = node.rightmost;
		constructor_initializer a1 = rewrite_constructor_initializer((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static statement rewrite_jump_statement(nonterminalState node) {
	if (node.queue != null) {
		return (statement)disambiguate.resolve(node, "jump-statement");
	}
	statement retval;
	switch (node.rule) {
	default: throw new System.Exception("jump-statement");

	case "jump-statement : break-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_break_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "jump-statement : continue-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_continue_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "jump-statement : goto-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_goto_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "jump-statement : return-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_return_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "jump-statement : throw-statement": {
		state tmp = node.rightmost;
		statement a1 = rewrite_throw_statement((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static switch_label rewrite_typeswitch_label(nonterminalState node) {
	if (node.queue != null) {
		return (switch_label)disambiguate.resolve(node, "typeswitch-label");
	}
	switch_label retval;
	switch (node.rule) {
	default: throw new System.Exception("typeswitch-label");

	case "typeswitch-label : case type :": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		type a2 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new typeswitch_label(a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_invocation_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "invocation-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("invocation-expression");

	case "invocation-expression : primary-expression ( argument-listopt )": {
		state tmp = node.rightmost;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a3 = rewrite_argument_listopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_primary_expression((nonterminalState)tmp);;

		retval = new invocation_expression(a1,a3);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_conditional_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "conditional-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("conditional-expression");

	case "conditional-expression : conditional-or-expression": {
		state tmp = node.rightmost;
		expression a1 = rewrite_conditional_or_expression((nonterminalState)tmp);;

		retval = a1;
		break;
		}

	case "conditional-expression : conditional-or-expression ? expression : expression": {
		state tmp = node.rightmost;
		expression a5 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a3 = rewrite_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a1 = rewrite_conditional_or_expression((nonterminalState)tmp);;

		retval = new cond_expression(a1,a3,a5);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static type rewrite_enum_baseopt(nonterminalState node) {
	if (node.queue != null) {
		return (type)disambiguate.resolve(node, "enum-baseopt");
	}
	type retval;
	switch (node.rule) {
	default: throw new System.Exception("enum-baseopt");

	case "enum-baseopt :": {

		retval = null;
		break;
		}

	case "enum-baseopt : enum-base": {
		state tmp = node.rightmost;
		type a1 = rewrite_enum_base((nonterminalState)tmp);;

		retval = a1;
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static IList rewrite_typeswitch_sections(nonterminalState node) {
	if (node.queue != null) {
		return (IList)disambiguate.resolve(node, "typeswitch-sections");
	}
	IList retval;
	switch (node.rule) {
	default: throw new System.Exception("typeswitch-sections");

	case "typeswitch-sections : typeswitch-section": {
		state tmp = node.rightmost;
		typeswitch_section a1 = rewrite_typeswitch_section((nonterminalState)tmp);;

		retval = typeswitch_sectionList.New(a1);
		break;
		}

	case "typeswitch-sections : typeswitch-sections typeswitch-section": {
		state tmp = node.rightmost;
		typeswitch_section a2 = rewrite_typeswitch_section((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_typeswitch_sections((nonterminalState)tmp);;

		retval = List.Cons(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_interface_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "interface-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("interface-declaration");

	case "interface-declaration : attributesopt member-modifiersopt interface identifier interface-baseopt interface-body ;opt": {
		state tmp = node.rightmost;
		InputElement a7 = rewrite_Aopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a6 = rewrite_interface_body((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a5 = rewrite_interface_baseopt((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a4 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new interface_declaration(a1,a2,a4,a5,a6);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static switch_label rewrite_switch_label(nonterminalState node) {
	if (node.queue != null) {
		return (switch_label)disambiguate.resolve(node, "switch-label");
	}
	switch_label retval;
	switch (node.rule) {
	default: throw new System.Exception("switch-label");

	case "switch-label : case constant-expression :": {
		state tmp = node.rightmost;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		expression a2 = rewrite_constant_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new switch_expression(a2);
		break;
		}

	case "switch-label : default :": {
		state tmp = node.rightmost;
		InputElement a2 = rewrite_terminal(tmp);
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new switch_default();
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static declaration rewrite_event_declaration(nonterminalState node) {
	if (node.queue != null) {
		return (declaration)disambiguate.resolve(node, "event-declaration");
	}
	declaration retval;
	switch (node.rule) {
	default: throw new System.Exception("event-declaration");

	case "event-declaration : attributesopt member-modifiersopt event type variable-declarators ;": {
		state tmp = node.rightmost;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a5 = rewrite_variable_declarators((nonterminalState)tmp);;
		tmp = tmp.below;
		type a4 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new event_declaration1(a1,a2,a4,a5);
		break;
		}

	case "event-declaration : attributesopt member-modifiersopt event type member-name { event-accessor-declarations }": {
		state tmp = node.rightmost;
		InputElement a8 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a7 = rewrite_event_accessor_declarations((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a6 = rewrite_terminal(tmp);
		tmp = tmp.below;
		member_name a5 = rewrite_member_name((nonterminalState)tmp);;
		tmp = tmp.below;
		type a4 = rewrite_type((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a3 = rewrite_terminal(tmp);
		tmp = tmp.below;
		IList a2 = rewrite_member_modifiersopt((nonterminalState)tmp);;
		tmp = tmp.below;
		IList a1 = rewrite_attributesopt((nonterminalState)tmp);;

		retval = new event_declaration2(a1,a2,a4,a5,a7);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static expression rewrite_pre_decrement_expression(nonterminalState node) {
	if (node.queue != null) {
		return (expression)disambiguate.resolve(node, "pre-decrement-expression");
	}
	expression retval;
	switch (node.rule) {
	default: throw new System.Exception("pre-decrement-expression");

	case "pre-decrement-expression : -- unary-expression": {
		state tmp = node.rightmost;
		expression a2 = rewrite_unary_expression((nonterminalState)tmp);;
		tmp = tmp.below;
		InputElement a1 = rewrite_terminal(tmp);

		retval = new pre_expression(a1,a2);
		break;
		}


	}
	IHasCoordinate ihc = ((object)retval) as IHasCoordinate;
	if (ihc != null) {
		ihc.begin = node.begin;
		ihc.end = node.end;
	}
	return retval;
}


public static InputElement rewrite_terminal(state node) {
	return ((terminalState)node).terminal;
}
}

