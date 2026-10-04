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
