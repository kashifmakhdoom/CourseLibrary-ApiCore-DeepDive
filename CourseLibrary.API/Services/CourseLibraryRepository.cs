using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.QueryParams;
using Microsoft.EntityFrameworkCore;

namespace CourseLibrary.API.Services;

public class CourseLibraryRepository : ICourseLibraryRepository
{
    private readonly CourseLibraryContext _context;
    private readonly IPropertyMappingService _propertyMappingService;

    public CourseLibraryRepository(CourseLibraryContext context,
        IPropertyMappingService propertyMappingService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _propertyMappingService = propertyMappingService;
    }

    public void AddCourse(Guid authorId, Course course)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        if (course == null)
        {
            throw new ArgumentNullException(nameof(course));
        }

        // always set the AuthorId to the passed-in authorId
        course.AuthorId = authorId;
        _context.Courses.Add(course);
    }

    public void DeleteCourse(Course course)
    {
        _context.Courses.Remove(course);
    }

    public async Task<Course> GetCourseAsync(Guid authorId, Guid courseId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        if (courseId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(courseId));
        }

#pragma warning disable CS8603 // Possible null reference return.
        return await _context.Courses
          .Where(c => c.AuthorId == authorId && c.Id == courseId).FirstOrDefaultAsync();
#pragma warning restore CS8603 // Possible null reference return.
    }

    public async Task<IEnumerable<Course>> GetCoursesAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        return await _context.Courses
                    .Where(c => c.AuthorId == authorId)
                    .OrderBy(c => c.Title).ToListAsync();
    }

    public void UpdateCourse(Course course)
    {
        // no code in this implementation
    }

    public void AddAuthor(Author author)
    {
        if (author == null)
        {
            throw new ArgumentNullException(nameof(author));
        }

        // the repository fills the id (instead of using identity columns)
        author.Id = Guid.NewGuid();

        foreach (var course in author.Courses)
        {
            course.Id = Guid.NewGuid();
        }

        _context.Authors.Add(author);
    }

    public async Task<bool> AuthorExistsAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

        return await _context.Authors.AnyAsync(a => a.Id == authorId);
    }

    public void DeleteAuthor(Author author)
    {
        if (author == null)
        {
            throw new ArgumentNullException(nameof(author));
        }

        _context.Authors.Remove(author);
    }

    public async Task<Author> GetAuthorAsync(Guid authorId)
    {
        if (authorId == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(authorId));
        }

#pragma warning disable CS8603 // Possible null reference return.
        return await _context.Authors.FirstOrDefaultAsync(a => a.Id == authorId);
#pragma warning restore CS8603 // Possible null reference return.
    }


    public async Task<IEnumerable<Author>> GetAuthorsAsync()
    {
        return await _context.Authors.ToListAsync();
    }

    // Filtering and Searching
    public async Task<PagedList<Author>> GetAuthorsAsync(AuthorsQueryParams queryParams)
    {

        if (queryParams == null)
        {
            throw new ArgumentNullException(nameof(queryParams));
        }

        /*
        if(string.IsNullOrWhiteSpace(queryParams.Category)
            && string.IsNullOrWhiteSpace(queryParams.SearchQuery))
        {
            return await GetAuthorsAsync();
        }
        */

        var collection = _context.Authors as IQueryable<Author>;

        // Filtering
        if (!string.IsNullOrWhiteSpace(queryParams.Category))
        {
            queryParams.Category = queryParams.Category.Trim();
            collection = collection.Where(a => a.MainCategory == queryParams.Category);
        }

        // Searching
        if (!string.IsNullOrWhiteSpace(queryParams.SearchQuery))
        {
            queryParams.SearchQuery = queryParams.SearchQuery.Trim();
            collection = collection.Where(a => a.MainCategory.Contains(queryParams.SearchQuery)
            || a.FirstName.Contains(queryParams.SearchQuery)
            || a.LastName.Contains(queryParams.SearchQuery));
        }

        // Ordering
        if (!string.IsNullOrWhiteSpace(queryParams.OrderBy))
        {
            // Get property mapping dictionary
            var authorPropertyMappingDictionary = _propertyMappingService
                .GetPropertyMapping<AuthorDto, Author>();

            collection = collection.ApplySort(queryParams.OrderBy, authorPropertyMappingDictionary);

            /*
            if(queryParams.OrderBy.ToLowerInvariant() == "name")
            {
                collection = collection.OrderBy(a => a.FirstName)
                .ThenBy(a => a.LastName);
            }
            */
        }

        return await PagedList<Author>.CreateAsync(collection,
        queryParams.PageNumber,
        queryParams.PageSize);

        /*
        return await collection
            .Skip(queryParams.PageSize * (queryParams.PageNumber - 1))
            .Take(queryParams.PageSize)
            .ToListAsync();
        */
    }

    public async Task<IEnumerable<Author>> GetAuthorsAsync(IEnumerable<Guid> authorIds)
    {
        if (authorIds == null)
        {
            throw new ArgumentNullException(nameof(authorIds));
        }

        return await _context.Authors.Where(a => authorIds.Contains(a.Id))
            .OrderBy(a => a.FirstName)
            .OrderBy(a => a.LastName)
            .ToListAsync();
    }

    public void UpdateAuthor(Author author)
    {
        // no code in this implementation
    }

    public async Task<bool> SaveAsync()
    {
        return (await _context.SaveChangesAsync() >= 0);
    }
}

