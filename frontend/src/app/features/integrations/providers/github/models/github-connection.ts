export type GitHubOwnerType = 'User' | 'Organization';

export interface GitHubConnection {
  id: string;
  teamId: string;
  owner: string;
  ownerType: GitHubOwnerType;
  createdAt: string;
  lastSyncAt: string | null;
}
