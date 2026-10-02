export interface GitHubSyncResponse {
  synchronized: number;
  created: number;
  updated: number;
  failedRequests: number;
  isComplete: boolean;
}
