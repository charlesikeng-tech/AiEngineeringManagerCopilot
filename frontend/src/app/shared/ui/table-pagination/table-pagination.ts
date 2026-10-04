import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { LocalizedNumberPipe } from '@core/i18n/localized-format.pipes';
import { NzPaginationModule } from 'ng-zorro-antd/pagination';
import { NzSelectModule } from 'ng-zorro-antd/select';

let nextPaginationId = 0;

@Component({
  selector: 'app-table-pagination',
  standalone: true,
  imports: [FormsModule, TranslatePipe, LocalizedNumberPipe, NzPaginationModule, NzSelectModule],
  templateUrl: './table-pagination.html',
  styleUrl: './table-pagination.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TablePagination {
  readonly pageNumber = input(1);
  readonly pageSize = input(10);
  readonly totalCount = input.required<number>();
  readonly disabled = input(false);
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
  readonly pageSizeOptions = [10, 20, 50];
  readonly pageSizeId = `table-page-size-${++nextPaginationId}`;
}
