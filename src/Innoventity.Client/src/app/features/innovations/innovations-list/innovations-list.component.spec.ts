import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { InnovationsListComponent } from './innovations-list.component';
import { InnovationsService } from '../../../core/services/innovations.service';
import { InnovationListResponse } from '../../../core/models/innovation.model';
import { Result } from '../../../core/models/result.model';

describe('InnovationsListComponent', () => {
  let component: InnovationsListComponent;
  let fixture: ComponentFixture<InnovationsListComponent>;
  let mockInnovationsService: { listInnovations: jest.Mock };
  let mockRouter: { navigate: jest.Mock };

  const mockListResponse: InnovationListResponse = {
    items: [
      {
        innovationId: 'inno-id-1',
        title: 'Smart Sensor Array',
        productType: 'Hardware',
        researchCategory: 'Engineering',
        status: 'Published',
        submittedAt: '2024-03-01T00:00:00Z',
        owner: { actorId: 'actor-1', firstName: 'Alice', lastName: 'Smith', displayName: 'Alice Smith' },
        targetIndustries: ['Technology', 'Healthcare'],
        partnersNeeded: []
      },
      {
        innovationId: 'inno-id-2',
        title: 'AI Diagnostics Platform',
        productType: 'Software',
        researchCategory: 'Engineering',
        status: 'Published',
        submittedAt: '2024-04-15T00:00:00Z',
        owner: { actorId: 'actor-2', firstName: 'Bob', lastName: 'Jones', displayName: 'Bob Jones' },
        targetIndustries: ['Healthcare'],
        partnersNeeded: []
      }
    ],
    totalCount: 2,
    page: 1,
    pageSize: 20
  };

  beforeEach(async () => {
    mockInnovationsService = { listInnovations: jest.fn() };
    mockRouter = { navigate: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [InnovationsListComponent],
      providers: [
        provideNoopAnimations(),
        { provide: InnovationsService, useValue: mockInnovationsService },
        { provide: Router, useValue: mockRouter }
      ]
    }).compileComponents();
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should create', () => {
    mockInnovationsService.listInnovations.mockResolvedValue({ success: true, data: mockListResponse });
    fixture = TestBed.createComponent(InnovationsListComponent);
    component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should start with loading = true and empty innovations', () => {
    // Never resolve so we can inspect the initial state
    mockInnovationsService.listInnovations.mockReturnValue(new Promise(() => {}));
    fixture = TestBed.createComponent(InnovationsListComponent);
    component = fixture.componentInstance;

    expect(component.loading()).toBe(true);
    expect(component.innovations()).toEqual([]);
    expect(component.error()).toBeNull();
  });

  describe('ngOnInit — success', () => {
    it('should populate innovations and totalCount when service returns data', async () => {
      mockInnovationsService.listInnovations.mockResolvedValue({
        success: true,
        data: mockListResponse
      } as Result<InnovationListResponse>);

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;

      await component.ngOnInit();
      fixture.detectChanges();

      expect(component.innovations()).toHaveLength(2);
      expect(component.totalCount()).toBe(2);
      expect(component.loading()).toBe(false);
      expect(component.error()).toBeNull();
    });

    it('should render innovation cards with titles', async () => {
      mockInnovationsService.listInnovations.mockResolvedValue({
        success: true,
        data: mockListResponse
      });

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.innovations.set(mockListResponse.items);
      component.totalCount.set(mockListResponse.totalCount);

      fixture.detectChanges();
      await fixture.whenStable();

      const content = fixture.nativeElement.textContent;
      expect(content).toContain('Smart Sensor Array');
      expect(content).toContain('AI Diagnostics Platform');
      expect(content).toContain('2 innovations found');
    });

    it('should show singular "innovation found" for count of 1', async () => {
      const singleResponse = { ...mockListResponse, items: [mockListResponse.items[0]], totalCount: 1 };
      mockInnovationsService.listInnovations.mockResolvedValue({ success: true, data: singleResponse });

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.innovations.set(singleResponse.items);
      component.totalCount.set(1);

      fixture.detectChanges();
      await fixture.whenStable();

      expect(fixture.nativeElement.textContent).toContain('1 innovation found');
    });

    it('should show "No Innovations Found" when items array is empty', async () => {
      const emptyResponse = { ...mockListResponse, items: [], totalCount: 0 };
      mockInnovationsService.listInnovations.mockResolvedValue({ success: true, data: emptyResponse });

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.innovations.set([]);
      component.totalCount.set(0);

      fixture.detectChanges();
      await fixture.whenStable();

      expect(fixture.nativeElement.textContent).toContain('No Innovations Found');
    });
  });

  describe('ngOnInit — error', () => {
    it('should set error and stop loading when service returns failure', async () => {
      mockInnovationsService.listInnovations.mockResolvedValue({
        success: false,
        error: 'Failed to load innovations.'
      });

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;

      await component.ngOnInit();
      fixture.detectChanges();

      expect(component.innovations()).toHaveLength(0);
      expect(component.error()).toBe('Failed to load innovations.');
      expect(component.loading()).toBe(false);
    });

    it('should use fallback error message when error field is undefined', async () => {
      mockInnovationsService.listInnovations.mockResolvedValue({ success: false });

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;

      await component.ngOnInit();

      expect(component.error()).toBe('Failed to load innovations.');
    });

    it('should render error card with error message', async () => {
      mockInnovationsService.listInnovations.mockResolvedValue({
        success: false,
        error: 'Network error'
      });

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.error.set('Network error');

      fixture.detectChanges();
      await fixture.whenStable();

      expect(fixture.nativeElement.textContent).toContain('Network error');
    });
  });

  describe('viewDetail', () => {
    it('should navigate to /innovations/:id when viewDetail is called', () => {
      mockInnovationsService.listInnovations.mockReturnValue(new Promise(() => {}));
      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;

      component.viewDetail('inno-id-1');

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/innovations', 'inno-id-1']);
    });

    it('should navigate to correct ID when View Details button is clicked', async () => {
      mockInnovationsService.listInnovations.mockResolvedValue({ success: true, data: mockListResponse });

      fixture = TestBed.createComponent(InnovationsListComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.innovations.set([mockListResponse.items[0]]);
      component.totalCount.set(1);

      fixture.detectChanges();
      await fixture.whenStable();

      const viewButton = fixture.nativeElement.querySelector('button');
      viewButton?.click();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/innovations', 'inno-id-1']);
    });
  });
});
