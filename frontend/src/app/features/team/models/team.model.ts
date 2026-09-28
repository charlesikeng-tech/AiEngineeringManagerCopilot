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

export type TeamMemberRole =
  | 'EngineeringManager'
  | 'Developer'
  | 'TechLead'
  | 'QA'
  | 'ProductManager'
  | 'DataEngineer'
  | 'Other';

export interface TeamMember {
  id: string;
  teamId: string;
  name: string;
  email: string;
  role: TeamMemberRole;
  providerUserId: string | null;
  createdAt: string;
}

export interface CreateTeamMemberRequest {
  name: string;
  email: string;
  role: TeamMemberRole;
  providerUserId: string | null;
}

export interface UpdateTeamMemberRequest {
  name: string;
  email: string;
  role: TeamMemberRole;
  providerUserId: string | null;
}
