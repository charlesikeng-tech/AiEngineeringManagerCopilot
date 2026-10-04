export interface Team {
  id: string;
  ownerUserId: string;
  name: string;
  description: string | null;
  createdAt: string;
}

export interface CreateTeamRequest {
  name: string;
  description: string | null;
}

export interface UpdateTeamRequest {
  name: string;
  description: string | null;
}
