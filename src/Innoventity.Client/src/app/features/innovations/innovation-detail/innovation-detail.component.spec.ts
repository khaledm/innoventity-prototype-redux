import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { InnovationDetailComponent } from './innovation-detail.component';
import { InnovationsService } from '../../../core/services/innovations.service';
import { InnovationDetail } from '../../../core/models/innovation.model';
import { Result } from '../../../core/models/result.model';

describe('InnovationDetailComponent', () => {
  let component: InnovationDetailComponent;
  let fixture: ComponentFixture<InnovationDetailComponent>;
  let mockInnovationsService: jest.Mocked<InnovationsService>;
  let mockRouter: jest.Mocked<Router>;
  let mockActivatedRoute: any;

  const mockInnovation: InnovationDetail = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    ideaToken: 'INN-2024-001',
    ownerId: '456e7890-e89b-12d3-a456-426614174000',
    owner: {
      id: '456e7890-e89b-12d3-a456-426614174000',
      firstName: 'Jane',
      lastName: 'Doe',
      displayName: 'Jane Doe',
      email: 'jane@example.com',
      actorType: 'IdeaGenerator'
    },
    title: 'Advanced AI System',
    productType: 'Software',
    researchBackground: 'This innovation represents a breakthrough in artificial intelligence...',
    researchCategory: 'Engineering',
    iprStatus: 'Patent Pending',
    productDescription: 'An advanced AI system for automated problem solving.',
    productAdvantages: 'Faster processing, lower cost, higher accuracy.',
    developmentPhase: 'Prototype',
    developmentProcess: 'Agile',
    targetMarket: 'Enterprise software market.',
    targetCustomerBase: 'Large corporations',
    targetCustomerType: 'B2B',
    productKeywords: 'AI, machine learning, automation',
    advantageKeywords: 'speed, accuracy, cost',
    status: 'Published',
    createdAt: '2024-01-01T00:00:00Z',
    submittedAt: '2024-01-15T00:00:00Z',
    targetIndustries: [{ industryId: 'TECH-001', name: 'Technology' }]
  };

  beforeEach(async () => {
    // Mock InnovationsService
    mockInnovationsService = {
      getInnovationById: jest.fn(),
      clearError: jest.fn()
    } as any;

    // Mock Router
    mockRouter = {
      navigate: jest.fn()
    } as any;

    // Mock ActivatedRoute
    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jest.fn()
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [InnovationDetailComponent],
      providers: [
        provideNoopAnimations(),
        { provide: InnovationsService, useValue: mockInnovationsService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should create', () => {
    fixture = TestBed.createComponent(InnovationDetailComponent);
    component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load innovation on init with valid ID', async () => {
      // Arrange
      const innovationId = '123e4567-e89b-12d3-a456-426614174000';
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue(innovationId);
      const successResult: Result<InnovationDetail> = {
        success: true,
        data: mockInnovation
      };
      mockInnovationsService.getInnovationById.mockResolvedValue(successResult);

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;

      // Act
      await component.ngOnInit();
      fixture.detectChanges();

      // Assert
      expect(mockActivatedRoute.snapshot.paramMap.get).toHaveBeenCalledWith('id');
      expect(mockInnovationsService.getInnovationById).toHaveBeenCalledWith(innovationId);
      expect(component.innovation()).toEqual(mockInnovation);
      expect(component.loading()).toBe(false);
      expect(component.error()).toBeNull();
    });

    it('should set error when ID is missing', async () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue(null);

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;

      // Act
      await component.ngOnInit();
      fixture.detectChanges();

      // Assert
      expect(component.error()).toBe('Invalid innovation ID');
      expect(component.loading()).toBe(false);
      expect(mockInnovationsService.getInnovationById).not.toHaveBeenCalled();
    });

    it('should set error when service returns failure', async () => {
      // Arrange
      const innovationId = '123e4567-e89b-12d3-a456-426614174000';
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue(innovationId);
      const failureResult: Result<InnovationDetail> = {
        success: false,
        error: 'Innovation not found'
      };
      mockInnovationsService.getInnovationById.mockResolvedValue(failureResult);

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;

      // Act
      await component.ngOnInit();
      fixture.detectChanges();

      // Assert
      expect(component.error()).toBe('Innovation not found');
      expect(component.loading()).toBe(false);
      expect(component.innovation()).toBeNull();
    });

    it('should set fallback error when service returns failure with no error string', async () => {
      const innovationId = '123e4567-e89b-12d3-a456-426614174000';
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue(innovationId);
      const failureResult: Result<InnovationDetail> = { success: false };
      mockInnovationsService.getInnovationById.mockResolvedValue(failureResult);

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;

      await component.ngOnInit();

      expect(component.error()).toBe('Failed to load innovation');
      expect(component.loading()).toBe(false);
    });

    it('should handle 404 error gracefully', async () => {
      // Arrange
      const innovationId = 'nonexistent-id';
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue(innovationId);
      const notFoundResult: Result<InnovationDetail> = {
        success: false,
        error: 'Innovation not found (404)'
      };
      mockInnovationsService.getInnovationById.mockResolvedValue(notFoundResult);

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;

      // Act
      await component.ngOnInit();
      fixture.detectChanges();

      // Assert
      expect(component.error()).toBe('Innovation not found (404)');
      expect(component.loading()).toBe(false);
    });
  });

  describe('UI rendering', () => {
    it('should display loading spinner initially', async () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      const pendingPromise = new Promise<Result<InnovationDetail>>(() => {
        // Never resolve to keep loading state
      });
      mockInnovationsService.getInnovationById.mockReturnValue(pendingPromise);

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;

      // Act
      fixture.detectChanges();
      await fixture.whenStable();

      const content = fixture.nativeElement.textContent;

      // Assert
      expect(content).toContain('Loading innovation details');
    });

    it('should display error message when error occurs', async () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      mockInnovationsService.getInnovationById.mockResolvedValue({
        success: false,
        error: 'Failed to load innovation'
      });

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.error.set('Failed to load innovation');
      component.innovation.set(null);

      // Act
      fixture.detectChanges();
      await fixture.whenStable();

      const content = fixture.nativeElement.textContent;

      // Assert
      expect(content).toContain('Failed to load innovation');
      expect(component.error()).toBe('Failed to load innovation');
    });

    it('should display innovation details when loaded', async () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      mockInnovationsService.getInnovationById.mockResolvedValue({
        success: true,
        data: mockInnovation
      });

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.error.set(null);
      component.innovation.set(mockInnovation);

      // Act
      fixture.detectChanges();
      await fixture.whenStable();

      const content = fixture.nativeElement.textContent;

      // Assert
      expect(content).toContain(mockInnovation.title);
      expect(content).toContain(mockInnovation.researchBackground);
      expect(component.innovation()).toEqual(mockInnovation);
    });

    it('should display IPR chip when iprStatus is not None', async () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      mockInnovationsService.getInnovationById.mockResolvedValue({
        success: true,
        data: mockInnovation
      });

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.error.set(null);
      component.innovation.set(mockInnovation);

      // Act
      fixture.detectChanges();
      await fixture.whenStable();

      const content = fixture.nativeElement.textContent;

      // Assert
      expect(content).toContain(mockInnovation.iprStatus); // 'Patent Pending'
      expect(component.innovation()?.iprStatus).toBe('Patent Pending');
    });

    it('should display iprStatus value in details', async () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      mockInnovationsService.getInnovationById.mockResolvedValue({
        success: true,
        data: mockInnovation
      });

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.error.set(null);
      component.innovation.set(mockInnovation);

      // Act
      fixture.detectChanges();
      await fixture.whenStable();

      const content = fixture.nativeElement.textContent;

      // Assert
      expect(content).toContain('Patent Pending');
      expect(component.innovation()?.iprStatus).toBeTruthy();
    });

    it('should not display IPR section when iprStatus is None', async () => {
      // Arrange
      const innovationWithoutIPR: InnovationDetail = {
        ...mockInnovation,
        iprStatus: 'None'
      };
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      mockInnovationsService.getInnovationById.mockResolvedValue({
        success: true,
        data: innovationWithoutIPR
      });

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.error.set(null);
      component.innovation.set(innovationWithoutIPR);

      // Act
      fixture.detectChanges();
      await fixture.whenStable();

      const iprSection = fixture.nativeElement.querySelector('.ipr-status');

      // Assert
      expect(iprSection).toBeNull();
      expect(component.innovation()?.iprStatus).toBe('None');
    });
  });

  describe('goBack', () => {
    it('should navigate to innovations list when goBack is called', () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;

      // Act
      component.goBack();

      // Assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/innovations']);
    });

    it('should navigate when back button is clicked', async () => {
      // Arrange
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('test-id');
      mockInnovationsService.getInnovationById.mockResolvedValue({
        success: true,
        data: mockInnovation
      });

      fixture = TestBed.createComponent(InnovationDetailComponent);
      component = fixture.componentInstance;
      component.loading.set(false);
      component.error.set(null);
      component.innovation.set(mockInnovation);

      // Act
      fixture.detectChanges();
      await fixture.whenStable();

      const buttons = fixture.nativeElement.querySelectorAll('button');
      const backButton = Array.from(buttons).find((btn: any) =>
        btn.textContent.toLowerCase().includes('back')
      );

      expect(backButton).toBeTruthy();
      (backButton as HTMLButtonElement).click();

      // Assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/innovations']);
    });
  });
});
