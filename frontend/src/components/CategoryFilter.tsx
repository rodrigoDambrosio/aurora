import React from 'react';
import type { EventCategoryDto } from '../services/apiService';
import './CategoryFilter.css';

interface CategoryFilterProps {
  categories: EventCategoryDto[];
  selectedCategoryId: string | null;
  onCategoryChange: (categoryId: string | null) => void;
}

export const CategoryFilter: React.FC<CategoryFilterProps> = ({
  categories,
  selectedCategoryId,
  onCategoryChange
}) => {
  return (
    <div className="category-filter">
      <button
        className={`category-filter-button ${selectedCategoryId === null ? 'active' : ''}`}
        onClick={() => onCategoryChange(null)}
      >
        <span className="category-filter-dot all-categories"></span>
        <span className="category-filter-text">Todas</span>
      </button>
      {categories.map(category => (
        <button
          key={category.id}
          className={`category-filter-button ${selectedCategoryId === category.id ? 'active' : ''}`}
          onClick={() => onCategoryChange(category.id)}
        >
          <span
            className="category-filter-dot"
            style={{ backgroundColor: category.color }}
          ></span>
          <span className="category-filter-text">{category.name}</span>
        </button>
      ))}
    </div>
  );
};
