export interface InnovationDetail {
  id: string;
  title: string;
  productType: string;
  researchBackground: string;
  researchCategory: ResearchCategory;
  hasIPR: boolean;
  iprDetails?: string | null;
}

export enum ResearchCategory {
  Management = 'Management',
  Engineering = 'Engineering',
  NaturalScience = 'NaturalScience'
}
