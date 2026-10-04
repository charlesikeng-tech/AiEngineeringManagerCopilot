import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideI18nTesting } from '@core/i18n/i18n-testing';
import { I18nService, LANGUAGE_STORAGE_KEY } from '@core/i18n/i18n.service';
import { NzPaginationComponent } from 'ng-zorro-antd/pagination';
import { NzSelectComponent } from 'ng-zorro-antd/select';
import { TablePagination } from './table-pagination';

describe('TablePagination', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      imports: [TablePagination],
      providers: provideI18nTesting(),
    }),
  );
  afterEach(() => localStorage.removeItem(LANGUAGE_STORAGE_KEY));

  async function render(totalCount = 25) {
    const fixture = TestBed.createComponent(TablePagination);
    fixture.componentRef.setInput('totalCount', totalCount);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture;
  }

  it('uses the same footer, sizes and localized range for every collection', async () => {
    const fixture = await render();
    expect(fixture.nativeElement.querySelector('.app-table-footer')).not.toBeNull();
    expect(fixture.componentInstance.pageSizeOptions).toEqual([10, 20, 50]);
    expect(fixture.nativeElement.textContent).toContain('Items per page');
    expect(fixture.nativeElement.textContent).toContain('of 25 items');
    const label = fixture.nativeElement.querySelector('label');
    expect(label.htmlFor).toBe(fixture.componentInstance.pageSizeId);
  });

  it('emits page and size changes without managing data or resetting parent context', async () => {
    const fixture = await render();
    const pageChange = vi.fn();
    const sizeChange = vi.fn();
    fixture.componentInstance.pageChange.subscribe(pageChange);
    fixture.componentInstance.pageSizeChange.subscribe(sizeChange);
    const pager = fixture.debugElement.query(By.directive(NzPaginationComponent)).componentInstance;
    pager.onPageIndexChange(2);
    fixture.debugElement
      .query(By.directive(NzSelectComponent))
      .triggerEventHandler('ngModelChange', 20);
    expect(pageChange).toHaveBeenCalledWith(2);
    expect(sizeChange).toHaveBeenCalledWith(20);
  });

  it('disables navigation while a new server page is loading', async () => {
    const fixture = await render();
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const pageChange = vi.fn();
    fixture.componentInstance.pageChange.subscribe(pageChange);
    const pager = fixture.debugElement.query(By.directive(NzPaginationComponent)).componentInstance;
    const selector = fixture.debugElement.query(By.directive(NzSelectComponent)).componentInstance;
    expect(selector.nzDisabled).toBe(true);
    pager.onPageIndexChange(2);
    expect(pageChange).not.toHaveBeenCalled();
  });

  it('hides an empty footer and responds to language changes', async () => {
    const fixture = await render(0);
    expect(fixture.nativeElement.querySelector('.app-table-footer')).toBeNull();
    fixture.componentRef.setInput('totalCount', 25);
    await TestBed.inject(I18nService).setLanguage('fr');
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Éléments par page');
    expect(fixture.nativeElement.textContent).toContain('sur 25 éléments');
  });
});
