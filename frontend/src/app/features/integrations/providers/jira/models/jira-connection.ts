export interface JiraConnection {
  id: string;
  teamId: string;
  baseUrl: string;
  email: string;
  projectKey: string;
  createdAt: string;
  lastSyncAt: string | null;
}

export interface CreateJiraConnectionRequest {
  baseUrl: string;
  email: string;
  apiToken: string;
  projectKey: string;
}

export interface TestJiraConnectionResponse {
  isValid: boolean;
  displayName: string | null;
  message: string;
}

export interface JiraSyncResult {
  created: number;
  updated: number;
  total: number;
}
