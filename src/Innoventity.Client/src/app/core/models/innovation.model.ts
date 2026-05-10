export interface OwnerDetail {
  id: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  actorType: string;
}

export interface OwnerListItem {
  actorId: string;
  firstName: string;
  lastName: string;
  displayName: string;
}

export interface IndustryRef {
  industryId: string;
  name: string;
}

export interface InnovationDetail {
  id: string;
  ideaToken: string;
  ownerId: string;
  owner: OwnerDetail | null;
  title: string;
  productType: string;
  researchBackground: string;
  researchCategory: string;
  iprStatus: string;
  productDescription: string;
  productAdvantages: string;
  developmentPhase: string;
  developmentProcess: string;
  targetMarket: string;
  targetCustomerBase: string;
  targetCustomerType: string;
  productKeywords: string;
  advantageKeywords: string;
  status: string;
  createdAt: string;
  submittedAt: string | null;
  targetIndustries: IndustryRef[];
}

export interface InnovationListItem {
  innovationId: string;
  title: string;
  productType: string;
  researchCategory: string;
  status: string;
  submittedAt: string | null;
  owner: OwnerListItem;
  targetIndustries: string[];
  partnersNeeded: string[];
}

export interface InnovationListResponse {
  items: InnovationListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** @deprecated Use researchCategory string from API directly */
export enum ResearchCategory {
  Management = 'Management',
  Engineering = 'Engineering',
  NaturalScience = 'NaturalScience'
}
