
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.QueryParams;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Dynamic;
using System.Text.Json;

namespace CourseLibrary.API.Controllers;

[ApiController]
[Route("api/{controller}")]
public class AuthorsController : ControllerBase
{
    private readonly ICourseLibraryRepository _courseLibraryRepository;
    private readonly IMapper _mapper;
    private readonly IPropertyMappingService _propertyMappingService;
    private readonly IPropertyCheckerService _propertyCheckerService;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    public AuthorsController(
        ICourseLibraryRepository courseLibraryRepository,
        IMapper mapper,
        IPropertyMappingService propertyMappingService,
        IPropertyCheckerService propertyCheckerService,
        ProblemDetailsFactory problemDetailsFactory)
    {
        _courseLibraryRepository = courseLibraryRepository ??
            throw new ArgumentNullException(nameof(courseLibraryRepository));
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
        _propertyMappingService = propertyMappingService ??
            throw new ArgumentNullException(nameof(propertyMappingService));
        _propertyCheckerService = propertyCheckerService ??
            throw new ArgumentNullException(nameof(propertyCheckerService));
        _problemDetailsFactory = problemDetailsFactory ??
            throw new ArgumentNullException(nameof(problemDetailsFactory));
    }

    [HttpGet(Name = "GetAuthors")]
    [HttpHead]
    // Complex types assumed to be coming from Request.Body. So [FromQuery] is used to explicitly bind from query
    //public async Task<ActionResult<IEnumerable<AuthorDto>>> GetAllAuthors(
    //[FromQuery] AuthorsQueryParams queryParams)
    public async Task<IActionResult> GetAllAuthors(
       [FromQuery] AuthorsQueryParams queryParams)
    {

        if(!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>(queryParams.OrderBy))
        {
            return BadRequest();
        }

        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>
              (queryParams.Fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    statusCode: 400,
                    detail: $"Not all requested data shaping fields exist on " +
                    $"the resource: {queryParams.Fields}"));
        }

        // Get authors from repo
        var authorsFromRepo = await _courseLibraryRepository
            .GetAuthorsAsync(queryParams);

        var previousPageLink = authorsFromRepo.HasPrevious
           ? CreateAuthorsResourceUri(
               queryParams,
               ResourceUriType.PreviousPage) : null;

        var nextPageLink = authorsFromRepo.HasNext
            ? CreateAuthorsResourceUri(
                queryParams,
                ResourceUriType.NextPage) : null;

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
            previousPageLink = previousPageLink,
            nextPageLink = nextPageLink
        };

        Response.Headers.Add("X-Pagination",
               JsonSerializer.Serialize(paginationMetadata));

        // return them
        return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .Project(queryParams.Fields));
    }

    [HttpGet("{authorId}", Name = "GetAuthor")]
    //public async Task<ActionResult<AuthorDto>> GetAuthor(Guid authorId)
    public async Task<IActionResult> GetAuthor(Guid authorId,
        string? fields)
    {
        // Get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        if (!_propertyCheckerService.TypeHasProperties<AuthorDto>
              (fields))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                    statusCode: 400,
                    detail: $"Not all requested data shaping fields exist on " +
                    $"the resource: {fields}"));
        }

        // Return author
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo)
            .Project(fields));
    }

    [HttpPost]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDTO author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        return CreatedAtRoute("GetAuthor",
            new { authorId = authorToReturn.Id },
            authorToReturn);
    }

    [HttpOptions()]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET, POST, HEAD, OPTIONS");
        return Ok();
    }

    private string? CreateAuthorsResourceUri(
        AuthorsQueryParams queryParams,
        ResourceUriType type)
    {
        switch (type)
        {
            case ResourceUriType.PreviousPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = queryParams.Fields,
                        orderBy = queryParams.OrderBy,
                        pageNumber = queryParams.PageNumber - 1,
                        pageSize = queryParams.PageSize,
                        category = queryParams.Category,
                        searchQuery = queryParams.SearchQuery
                    });
            case ResourceUriType.NextPage:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = queryParams.Fields,
                        orderBy = queryParams.OrderBy,
                        pageNumber = queryParams.PageNumber + 1,
                        pageSize = queryParams.PageSize,
                        category = queryParams.Category,
                        searchQuery = queryParams.SearchQuery
                    });
            default:
                return Url.Link("GetAuthors",
                    new
                    {
                        fields = queryParams.Fields,
                        orderBy = queryParams.OrderBy,
                        pageNumber = queryParams.PageNumber,
                        pageSize = queryParams.PageSize,
                        category = queryParams.Category,
                        searchQuery = queryParams.SearchQuery
                    });
        }
    }
}



// Status Codes Standard: RFC-9110
// Problem Detail Standard: RFC-7807
