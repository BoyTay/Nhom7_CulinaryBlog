type PaginationProps = {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  label?: string;
  disabled?: boolean;
};

/** Page numbers to show: first, last, and a window around the current page; null marks a gap. */
function visiblePages(current: number, last: number): (number | null)[] {
  const wanted = new Set([1, last, current - 1, current, current + 1]);
  const pages = [...wanted].filter((page) => page >= 1 && page <= last).sort((a, b) => a - b);
  const result: (number | null)[] = [];
  pages.forEach((page, index) => {
    if (index > 0 && page - pages[index - 1] > 1) result.push(null);
    result.push(page);
  });
  return result;
}

export function Pagination({ page, totalPages, onPageChange, label = "Phân trang", disabled = false }: PaginationProps) {
  const lastPage = Math.max(totalPages, 1);
  const current = Math.min(Math.max(page, 1), lastPage);
  return (
    <nav className="ui-pagination" aria-label={label}>
      <button type="button" aria-label="Trang trước" disabled={disabled || current <= 1} onClick={() => onPageChange(current - 1)}>
        ‹
      </button>
      {visiblePages(current, lastPage).map((item, index) =>
        item === null ? (
          <span className="ui-pagination-gap" key={`gap-${index}`} aria-hidden="true">…</span>
        ) : (
          <button
            type="button"
            key={item}
            className={item === current ? "ui-pagination-current" : undefined}
            aria-current={item === current ? "page" : undefined}
            aria-label={`Trang ${item}`}
            disabled={disabled}
            onClick={() => item !== current && onPageChange(item)}
          >
            {item}
          </button>
        ),
      )}
      <button type="button" aria-label="Trang sau" disabled={disabled || current >= lastPage} onClick={() => onPageChange(current + 1)}>
        ›
      </button>
    </nav>
  );
}
