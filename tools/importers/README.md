# Importers

Importers should convert source data into Epsilon's canonical formats.

Examples:

- furnidata importers
- external texts and vars importers
- SQL extractors and migration helpers
- room and item migration helpers
- public-room asset manifest builders
- client asset root inventory builders
- client build manifest builders
- avatar asset manifest builders
- figure-data manifest builders
- launcher profile manifest builders
- item-definition manifest builders

Rules:

- importers may read source files
- importers must emit canonical Epsilon data formats
- runtime services must not depend on raw source layouts or archive URLs

Collection and download orchestration belong in `tools/ingest`, not here.
