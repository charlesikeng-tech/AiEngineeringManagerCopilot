export interface EngineeringAction {
  id: string;
  reportId: string;
  metricType: string | null;
  title: string;
  description: string;
  priority: ActionPriority;
  owner: string | null;
  dueDate: string | null;
  status: ActionStatus;
  createdAt: string;
}

export interface EngineeringActionsResponse {
  teamId: string;
  reportId: string;
  periodStart: string;
  periodEnd: string;
  actions: EngineeringAction[];
}

export interface UpdateEngineeringActionRequest {
  status?: ActionStatus;
  owner?: string | null;
  dueDate?: string | null;
}

export type ActionStatus = 'Todo' | 'InProgress' | 'Done' | 'Cancelled';

export type ActionPriority = 'Low' | 'Medium' | 'High' | 'Critical';
