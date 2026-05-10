import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { InnovationsService } from './innovations.service';
import { InnovationDetail, InnovationListResponse } from '../models/innovation.model';

describe('InnovationsService', () => {
  let service: InnovationsService;
  let httpMock: HttpTestingController;

  const mockInnovation: InnovationDetail = {
    id: 'test-id-123',
    ideaToken: 'INN-001',
    ownerId: 'owner-id',
    owner: {
      id: 'owner-id',
      firstName: 'Jane',
      lastName: 'Doe',
      displayName: 'Jane Doe',
      email: 'jane@example.com',
      actorType: 'IdeaGenerator'
    },
    title: 'Test Innovation',
    productType: 'Software',
    researchBackground: 'Background info',
    researchCategory: 'Engineering',
    iprStatus: 'Patent Pending',
    productDescription: 'A test product',
    productAdvantages: 'Advantage 1',
    developmentPhase: 'Prototype',
    developmentProcess: 'Agile',
    targetMarket: 'Enterprise',
    targetCustomerBase: 'Large corps',
    targetCustomerType: 'B2B',
    productKeywords: 'AI, ML',
    advantageKeywords: 'speed, accuracy',
    status: 'Published',
    createdAt: '2024-01-01T00:00:00Z',
    submittedAt: '2024-01-15T00:00:00Z',
    targetIndustries: [{ industryId: 'TECH-001', name: 'Technology' }]
  };

  const mockListResponse: InnovationListResponse = {
    items: [
      {
        innovationId: 'item-id-1',
        title: 'Innovation One',
        productType: 'Hardware',
        researchCategory: 'Engineering',
        status: 'Published',
        submittedAt: '2024-01-15T00:00:00Z',
        owner: { actorId: 'actor-1', firstName: 'Alice', lastName: 'Smith', displayName: 'Alice Smith' },
        targetIndustries: ['Technology'],
        partnersNeeded: []
      }
    ],
    totalCount: 1,
    page: 1,
    pageSize: 20
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [InnovationsService]
    });
    service = TestBed.inject(InnovationsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should expose readonly signals', () => {
    expect(service.loading()).toBe(false);
    expect(service.error()).toBeNull();
    expect(service.currentInnovation()).toBeNull();
  });

  describe('listInnovations', () => {
    it('should GET /innovations with no params and return success', async () => {
      const listPromise = service.listInnovations();
      const req = httpMock.expectOne('/innovations');
      expect(req.request.method).toBe('GET');
      req.flush(mockListResponse);

      const result = await listPromise;
      expect(result.success).toBe(true);
      expect(result.data).toEqual(mockListResponse);
    });

    it('should append query params when provided', async () => {
      const listPromise = service.listInnovations({
        industryId: 'HLTH-001',
        researchCategory: 'Engineering',
        page: 2,
        pageSize: 10
      });

      const req = httpMock.expectOne(r =>
        r.url === '/innovations' &&
        r.params.get('industryId') === 'HLTH-001' &&
        r.params.get('researchCategory') === 'Engineering' &&
        r.params.get('page') === '2' &&
        r.params.get('pageSize') === '10'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockListResponse);

      const result = await listPromise;
      expect(result.success).toBe(true);
    });

    it('should return "Please log in" message on 401', async () => {
      const listPromise = service.listInnovations();
      httpMock.expectOne('/innovations').flush(
        {},
        { status: 401, statusText: 'Unauthorized' }
      );

      const result = await listPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Please log in to view innovations.');
    });

    it('should return generic error message on other failures', async () => {
      const listPromise = service.listInnovations();
      httpMock.expectOne('/innovations').flush(
        {},
        { status: 500, statusText: 'Server Error' }
      );

      const result = await listPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Failed to load innovations.');
    });
  });

  describe('getInnovationById', () => {
    it('should GET /innovations/:id, set signals, and return success', async () => {
      const getPromise = service.getInnovationById('test-id-123');

      expect(service.loading()).toBe(true);

      const req = httpMock.expectOne('/innovations/test-id-123');
      expect(req.request.method).toBe('GET');
      req.flush(mockInnovation);

      const result = await getPromise;
      expect(result.success).toBe(true);
      expect(result.data).toEqual(mockInnovation);
      expect(service.currentInnovation()).toEqual(mockInnovation);
      expect(service.loading()).toBe(false);
      expect(service.error()).toBeNull();
    });

    it('should set loading to true before request and false after', async () => {
      const getPromise = service.getInnovationById('test-id-123');
      expect(service.loading()).toBe(true);

      httpMock.expectOne('/innovations/test-id-123').flush(mockInnovation);

      await getPromise;
      expect(service.loading()).toBe(false);
    });

    it('should return "Innovation not found" on 404', async () => {
      const getPromise = service.getInnovationById('unknown-id');
      httpMock.expectOne('/innovations/unknown-id').flush(
        {},
        { status: 404, statusText: 'Not Found' }
      );

      const result = await getPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Innovation not found.');
      expect(service.error()).toBe('Innovation not found.');
      expect(service.loading()).toBe(false);
    });

    it('should return "Please log in" on 401', async () => {
      const getPromise = service.getInnovationById('test-id-123');
      httpMock.expectOne('/innovations/test-id-123').flush(
        {},
        { status: 401, statusText: 'Unauthorized' }
      );

      const result = await getPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Please log in to view this innovation.');
    });

    it('should return "no permission" message on 403', async () => {
      const getPromise = service.getInnovationById('test-id-123');
      httpMock.expectOne('/innovations/test-id-123').flush(
        {},
        { status: 403, statusText: 'Forbidden' }
      );

      const result = await getPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('You do not have permission to view this innovation.');
    });

    it('should return generic error on other failures', async () => {
      const getPromise = service.getInnovationById('test-id-123');
      httpMock.expectOne('/innovations/test-id-123').flush(
        {},
        { status: 500, statusText: 'Server Error' }
      );

      const result = await getPromise;
      expect(result.success).toBe(false);
      expect(result.error).toBe('Failed to load innovation.');
    });
  });

  describe('clearError', () => {
    it('should reset the error signal to null', async () => {
      const getPromise = service.getInnovationById('unknown-id');
      httpMock.expectOne('/innovations/unknown-id').flush(
        {},
        { status: 404, statusText: 'Not Found' }
      );
      await getPromise;
      expect(service.error()).not.toBeNull();

      service.clearError();
      expect(service.error()).toBeNull();
    });
  });
});
