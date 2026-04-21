# Importers

Importers should convert legacy reference data into Epsilon's canonical formats.

Examples:

- furnidata importers
- external texts and vars importers
- old emulator SQL extractors
- room and item migration helpers
- public-room asset manifest builders

Rules:

- importers may read legacy source files
- importers must emit canonical Epsilon data formats
- runtime services must not depend on archive URLs or raw legacy package structures
