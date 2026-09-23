# ADR-0005: Shared contract baseline

- Status: Accepted
- Owner: Phan Trung Hiếu (2312616)
- Version: 1.0.0
- Date: 2026-09-23

## Decision

The platform/Category stream owns these shared contracts: `CategoryId` is a non-empty `Guid` referring to `Categories.Id`; `AuthorId` is a non-empty ASP.NET Identity user ID string; roles are `Admin` and `Author`; `PagedResult<T>` is the canonical paged-read envelope; and API failures use RFC 7807 Problem Details with stable machine-readable error codes.

Category is authoritative for category metadata. `Name` and `Slug` are unique. `Slug` is immutable after creation so public URLs remain stable. `OrderIndex` is non-negative and controls navigation ordering.

## Change process

An incompatible shared-contract change requires an ADR/SRS/API-contract update by its owner before consumers change code. The owner publishes a baseline commit SHA; consumers merge that baseline once and must not duplicate the model locally.
