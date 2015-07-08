declare module 'parse' {
	declare class Parse {
		static initialize (a: string, b: string) : void;
		/*
		export class Object {
			static extend (table: string) : Parse_Object;
		}
		*/
		static Object: typeof Parse_Object;
		static Query: typeof Parse_Query;
	}

	declare class Parse_Object {
		static extend (table: string) : Parse_Object;
		id: string;
		get (key: string) : any;
	}

	declare class Parse_Query {
		constructor (c: Parse_Object) : void;
		equalTo (key: string, value: any) : Parse_Query;
		notEqualTo (key: string, value: any) : Parse_Query;
		include (key: string) : Parse_Query;
		containedIn (key: string, value: Array<Parse_Object>) : Parse_Query;
	}
}
