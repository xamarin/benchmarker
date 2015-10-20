declare class Repo {
	getPull (id: number, callback: (err: any, info: Object) => void);
}

declare class GitHub {
	constructor (args: { username?: string, password?: string, token?: string, auth: string });
	getRepo (user: string, repo: string) : Repo; 
}

declare module "github-api" {
	export = GitHub;
}
