export interface User {
  actorId: string;
  fullName: string;
  actorType: ActorType;
}

export enum ActorType {
  IdeaGenerator = 'IdeaGenerator',
  Investor = 'Investor',
  RDOrganization = 'RDOrganization',
  Manufacturing = 'Manufacturing',
  SalesMarketing = 'SalesMarketing'
}

export interface LoginRequest {
  email: string;
  password: string;
  actorType: ActorType;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  actor: User;
}
