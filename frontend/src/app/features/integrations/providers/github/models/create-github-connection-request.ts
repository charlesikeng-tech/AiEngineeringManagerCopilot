import { GitHubOwnerType } from './github-connection';

export interface CreateGitHubConnectionRequest {
  owner: string;
  ownerType: GitHubOwnerType;
  accessToken: string;
}
