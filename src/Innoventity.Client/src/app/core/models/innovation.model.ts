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

export interface IdeaSummarySection {
  title: string;
  productType: string;
  researchBackground: string;
  researchCategory: string;
  iprStatus: string;
}

export interface ProductSection {
  productDescription: string;
  technologyDescription: string;
  productAdvantages: string;
  developmentPhase: string;
  developmentProcess: string;
  targetBeneficiaries: string;
  productKeywords: string;
  advantageKeywords: string;
}

export interface MarketSection {
  targetMarket: string;
  targetCustomerBase: string;
  targetCustomerType: string;
  relevantMarketSize: number | null;
  potentialMarketSize: number | null;
}

export interface CollaborationRequirementSection {
  partnersNeeded: string | null;
}

export interface IdeaSummaryListItem {
  title: string;
  productType: string;
  researchCategory: string;
}

export interface InnovationDetail {
  id: string;
  ideaToken: string;
  ownerId: string;
  owner: OwnerDetail | null;
  ideaSummary: IdeaSummarySection;
  product: ProductSection;
  market: MarketSection;
  collaborationRequirement: CollaborationRequirementSection;
  status: string;
  createdAt: string;
  submittedAt: string | null;
  targetIndustries: IndustryRef[];
}

export interface InnovationListItem {
  innovationId: string;
  ideaSummary: IdeaSummaryListItem;
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
