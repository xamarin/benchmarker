declare module "github-api" {
    declare class GitHub {
        constructor (options: Object): void;
        getRepo (user: string, repo: string): GitHubRepo;
    }

    declare class GitHubRepo {
        getPull (num: number, callback: (error: Object, info: Object) => void): void;
    }

    declare var exports: typeof GitHub;
}
