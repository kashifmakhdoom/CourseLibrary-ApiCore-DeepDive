
using AutoMapper;
using CourseLibrary.API.Entities;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.QueryParams;
using CourseLibrary.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Net.Http.Headers;
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
        /*
        var previousPageLink = authorsFromRepo.HasPrevious
           ? CreateAuthorsResourceUri(
               queryParams,
               ResourceUriType.PreviousPage) : null;

        var nextPageLink = authorsFromRepo.HasNext
            ? CreateAuthorsResourceUri(
                queryParams,
                ResourceUriType.NextPage) : null;
        */

        var paginationMetadata = new
        {
            totalCount = authorsFromRepo.TotalCount,
            pageSize = authorsFromRepo.PageSize,
            currentPage = authorsFromRepo.CurrentPage,
            totalPages = authorsFromRepo.TotalPages,
            //previousPageLink = previousPageLink,
            //nextPageLink = nextPageLink
        };

        Response.Headers.Add("X-Pagination",
               JsonSerializer.Serialize(paginationMetadata));

        // Create links
        var links = CreateLinksForAuthors(queryParams, 
            authorsFromRepo.HasNext, 
            authorsFromRepo.HasPrevious);

        var projectedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
            .Project(queryParams.Fields);

        var projectedAuthorsWithLinks = projectedAuthors.Select(author =>
        {
            var authorAsDictionary = author as IDictionary<string, object?>;
            var authorLinks = CreateLinksForAuthor(
                (Guid)authorAsDictionary["Id"],
                null);
            authorAsDictionary.Add("links", authorLinks);
            return authorAsDictionary;
        });

        var linkedCollectionResource = new
        {
            value = projectedAuthorsWithLinks,
            links = links
        };

        return Ok(linkedCollectionResource);

        // return them
        //return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
        //.Project(queryParams.Fields));
    }

    [Produces("application/json", "application/vnd.marvin.hateoas+json")]
    [HttpGet("{authorId}", Name = "GetAuthor")]
    public async Task<IActionResult> GetAuthor(Guid authorId,
        string? fields,
        [FromHeader(Name ="Accept")] string? mediaType)
    {

        //Check if inputted media type is valid media type
        if(!MediaTypeHeaderValue.TryParse(mediaType, out var parsedMediaType))
        {
            return BadRequest(
                _problemDetailsFactory.CreateProblemDetails(HttpContext,
                statusCode: 400,
                detail: $"Accept header media type value is not a valid media type"
            ));
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

        // Get author from repo
        var authorFromRepo = await _courseLibraryRepository.GetAuthorAsync(authorId);

        if (authorFromRepo == null)
        {
            return NotFound();
        }

        if (parsedMediaType.MediaType == "application/vnd.marvin.hateoas+json")
        {
            // Create links
            var links = CreateLinksForAuthor(authorId, fields);

            // Add links
            var linkedResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
                .Project(fields) as IDictionary<string, object?>;
            linkedResourceToReturn.Add("links", links);

            // Return author with links
            return Ok(linkedResourceToReturn);
        }
       
        // Return author
        return Ok(_mapper.Map<AuthorDto>(authorFromRepo)
            .Project(fields));
    }

    [HttpPost(Name = "CreateAuthor")]
    public async Task<ActionResult<AuthorDto>> CreateAuthor(AuthorForCreationDTO author)
    {
        var authorEntity = _mapper.Map<Entities.Author>(author);

        _courseLibraryRepository.AddAuthor(authorEntity);
        await _courseLibraryRepository.SaveAsync();

        var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

        // Create links
        var links = CreateLinksForAuthor(authorToReturn.Id, null);

        // Add links
        var linkedResourceToReturn = authorToReturn
            .Project(null) as IDictionary<string, object?>;
        linkedResourceToReturn.Add("links", links);

        return CreatedAtRoute("GetAuthor",
            new { authorId = linkedResourceToReturn["Id"] },
            linkedResourceToReturn);

        //return CreatedAtRoute("GetAuthor",
        //new { authorId = authorToReturn.Id },
        //authorToReturn);
    }

    [HttpOptions()]
    public IActionResult GetAuthorsOptions()
    {
        Response.Headers.Add("Allow", "GET, POST, HEAD, OPTIONS");
        return Ok();
    }

    private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId,
       string? fields)
    {
        var links = new List<LinkDto>();

        if (string.IsNullOrWhiteSpace(fields))
        {
            links.Add(
              new(Url.Link("GetAuthor", new { authorId }),
              "self",
              "GET"));
        }
        else
        {
            links.Add(
              new(Url.Link("GetAuthor", new { authorId, fields }),
              "self",
              "GET"));
        }

        links.Add(
              new(Url.Link("CreateCourseForAuthor", new { authorId }),
              "create_course_for_author",
              "POST"));
        links.Add(
             new(Url.Link("GetCoursesForAuthor", new { authorId }),
             "courses",
             "GET"));

        return links;
    }
    private IEnumerable<LinkDto> CreateLinksForAuthors(
        AuthorsQueryParams authorsResourceParameters,
        bool hasNext, bool hasPrevious)
    {
        var links = new List<LinkDto>();

        // self 
        links.Add(
            new(CreateAuthorsResourceUri(authorsResourceParameters,
                ResourceUriType.Current),
                "self",
                "GET"));

        if (hasNext)
        {
            links.Add(
                new(CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.NextPage),
                "nextPage",
                "GET"));
        }

        if (hasPrevious)
        {
            links.Add(
                new(CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.PreviousPage),
                "previousPage",
                "GET"));
        }

        return links;
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
            case ResourceUriType.Current:
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
