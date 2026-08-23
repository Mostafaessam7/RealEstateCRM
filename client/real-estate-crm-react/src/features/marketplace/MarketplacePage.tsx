import { useState } from "react";
import { motion } from "framer-motion";
import { Building, MapPin, BedDouble, Bath, Ruler, Search } from "lucide-react";
import { AsyncState } from "../../components/AsyncState";
import { CardGridSkeleton } from "../../components/Skeleton";
import { Pagination } from "../../components/Pagination";
import type { PublicUnitListQuery } from "../../types/marketplace";
import { usePublicUnits } from "./marketplaceApi";

export function MarketplacePage() {
  const [query, setQuery] = useState<PublicUnitListQuery>({ page: 1, pageSize: 12 });
  const [searchInput, setSearchInput] = useState("");
  const { data, isLoading, isError } = usePublicUnits(query);

  const submitSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setQuery((q) => ({ ...q, search: searchInput || undefined, page: 1 }));
  };

  return (
    <div style={{ minHeight: "100vh", background: "var(--color-bg)" }}>
      <header
        style={{
          background: "var(--gradient-sidebar)",
          padding: "64px 24px 56px",
          textAlign: "center",
          position: "relative",
          overflow: "hidden",
        }}
      >
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: "radial-gradient(circle at 30% 20%, rgba(139,92,246,0.35), transparent 55%)",
          }}
        />
        <div style={{ position: "relative", zIndex: 1 }}>
          <div style={{ display: "inline-flex", alignItems: "center", gap: 8, color: "#fff", opacity: 0.85, fontSize: 13, marginBottom: 12 }}>
            <Building size={16} /> Mecodex Marketplace
          </div>
          <h1 style={{ color: "#fff", fontFamily: "var(--font-display)", fontSize: 34, margin: "0 0 8px" }}>
            Find your next property
          </h1>
          <p style={{ color: "rgba(255,255,255,0.72)", maxWidth: 480, margin: "0 auto 28px" }}>
            Browse available units listed publicly by real estate companies on the platform.
          </p>

          <form
            onSubmit={submitSearch}
            style={{ display: "flex", justifyContent: "center", gap: 8, maxWidth: 480, margin: "0 auto" }}
          >
            <div style={{ position: "relative", flex: 1 }}>
              <Search size={15} color="var(--color-text-faint)" style={{ position: "absolute", left: 14, top: "50%", transform: "translateY(-50%)" }} />
              <input
                className="input"
                style={{ paddingLeft: 36, background: "#fff" }}
                placeholder="Search by location or unit code…"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
              />
            </div>
            <button type="submit" className="btn btn-primary">
              Search
            </button>
          </form>
        </div>
      </header>

      <main style={{ maxWidth: 1080, margin: "0 auto", padding: "36px 24px 64px" }}>
        <AsyncState
          isLoading={isLoading}
          isError={isError}
          errorMessage="Failed to load listings."
          isEmpty={!isLoading && (data?.items.length ?? 0) === 0}
          emptyTitle="No listings found"
          emptyMessage="Try a different search or check back later."
          skeleton={<CardGridSkeleton count={6} />}
        >
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))", gap: 18 }}>
            {data?.items.map((unit, index) => (
              <motion.div
                key={unit.unitId}
                className="card"
                initial={{ opacity: 0, y: 14 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3, delay: index * 0.04, ease: [0.22, 1, 0.36, 1] }}
                whileHover={{ y: -3 }}
              >
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                  <span className="badge badge-info">{unit.propertyType ?? "Unit"}</span>
                  <span style={{ fontSize: 11.5, color: "var(--color-text-faint)" }}>{unit.companyName}</span>
                </div>

                <div style={{ fontFamily: "var(--font-display)", fontSize: 22, fontWeight: 700, marginTop: 12 }}>
                  ${unit.price.toLocaleString()}
                </div>
                <div style={{ fontSize: 13, color: "var(--color-text-muted)", marginTop: 2 }}>{unit.projectName}</div>

                <div style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 12.5, color: "var(--color-text-muted)", marginTop: 10 }}>
                  <MapPin size={13} /> {unit.location ?? "Location on request"}
                </div>

                <div style={{ display: "flex", gap: 14, marginTop: 12, fontSize: 12.5, color: "var(--color-text-muted)" }}>
                  {unit.bedrooms != null && (
                    <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
                      <BedDouble size={13} /> {unit.bedrooms}
                    </span>
                  )}
                  {unit.bathrooms != null && (
                    <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
                      <Bath size={13} /> {unit.bathrooms}
                    </span>
                  )}
                  {unit.area != null && (
                    <span style={{ display: "flex", alignItems: "center", gap: 4 }}>
                      <Ruler size={13} /> {unit.area} m²
                    </span>
                  )}
                </div>

                {unit.description && (
                  <p style={{ fontSize: 12.5, color: "var(--color-text-muted)", marginTop: 10, marginBottom: 0 }}>
                    {unit.description.length > 100 ? `${unit.description.slice(0, 100)}…` : unit.description}
                  </p>
                )}
              </motion.div>
            ))}
          </div>

          {data && data.totalPages > 1 && (
            <div style={{ marginTop: 24 }}>
              <Pagination page={data.page} totalPages={data.totalPages} onPageChange={(page) => setQuery((q) => ({ ...q, page }))} />
            </div>
          )}
        </AsyncState>
      </main>
    </div>
  );
}
