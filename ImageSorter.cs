using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace Odland.Software.ImageSorter;

/// <summary>
/// Handles sorting and organizing images based on configurable criteria and directory structures.
/// </summary>
public class ImageSorter
{
    private readonly object _lockObject = new();
    private bool _isSorting;

    // Supported image extensions
    private static readonly string[] SupportedImageExtensions = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp",
        ".nef",  // Nikon RAW
        ".cr2",  // Canon RAW
        ".arw",  // Sony RAW
        ".dng",  // Adobe DNG
        ".raw"   // Generic RAW
    };

    #region Properties

    /// <summary>Gets or sets the source directory containing images to be sorted.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Gets or sets the target directory where sorted images will be placed.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Gets or sets the criteria by which images will be sorted (e.g., "date", "name", "size").</summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the directory structure for organized images (e.g., "YYYY\\MM\\").</summary>
    public string Structure { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether images should be renamed during sorting.</summary>
    public bool Rename { get; set; }

    /// <summary>Gets or sets a value indicating whether existing files should be overwritten.</summary>
    public bool Overwrite { get; set; }

    /// <summary>Gets or sets a value indicating whether to keep the original files (copy instead of move).</summary>
    public bool KeepOriginal { get; set; }

    /// <summary>Gets a value indicating whether sorting is currently in progress.</summary>
    public bool IsSorting
    {
        get
        {
            lock (_lockObject)
            {
                return _isSorting;
            }
        }
    }

    #endregion

    #region Events

    /// <summary>Raised when sorting starts.</summary>
    public event ImageSorterEventHandler? SortingStarted;

    /// <summary>Raised when sorting completes successfully.</summary>
    public event ImageSorterEventHandler? SortingCompleted;

    /// <summary>Raised when an individual image is sorted.</summary>
    public event ImageSorterEventHandler? ImageSorted;

    /// <summary>Raised when an error occurs during sorting.</summary>
    public event ImageSorterEventHandler? ErrorOccurred;

    /// <summary>Raised to provide progress updates during sorting.</summary>
    public event ImageSorterEventHandler? SortingProgressUpdated;

    /// <summary>Delegate for handling ImageSorter events.</summary>
    public delegate void ImageSorterEventHandler(object sender, ImageSorterEventArgs e);

    /// <summary>Event arguments for ImageSorter events.</summary>
    public class ImageSorterEventArgs : EventArgs
    {
        /// <summary>Gets the event message.</summary>
        public string Message { get; }

        /// <summary>Gets the path of the image being processed (old path).</summary>
        public string ImagePath { get; }

        /// <summary>Gets the new path of the image after sorting (new path with filename).</summary>
        public string NewImagePath { get; }

        /// <summary>Gets the progress percentage (0-100).</summary>
        public int Progress { get; }

        public ImageSorterEventArgs(string message, int progress, string imagePath = "", string newImagePath = "")
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Progress = Math.Clamp(progress, 0, 100);
            ImagePath = imagePath ?? string.Empty;
            NewImagePath = newImagePath ?? string.Empty;
        }
    }

    #endregion

    #region Constructors

    /// <summary>Initializes a new instance of the ImageSorter class with default values.</summary>
    public ImageSorter()
    {
    }

    /// <summary>Initializes a new instance of the ImageSorter class with specified configuration.</summary>
    public ImageSorter(string source, string target, string sortBy, string structure, bool rename = false, bool overwrite = false, bool keepOriginal = false)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        SortBy = sortBy ?? throw new ArgumentNullException(nameof(sortBy));
        Structure = structure ?? throw new ArgumentNullException(nameof(structure));
        Rename = rename;
        Overwrite = overwrite;
        KeepOriginal = keepOriginal;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when required properties are not set.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Source))
            throw new InvalidOperationException("Source directory must be specified.");

        if (string.IsNullOrWhiteSpace(Target))
            throw new InvalidOperationException("Target directory must be specified.");

        if (string.IsNullOrWhiteSpace(SortBy))
            throw new InvalidOperationException("SortBy criteria must be specified.");

        if (!System.IO.Directory.Exists(Source))
            throw new InvalidOperationException($"Source directory does not exist: {Source}");
    }

    /// <summary>
    /// Starts the image sorting process asynchronously.
    /// </summary>
    /// <param name="dryRun">If true, performs a dry run without actually moving files.</param>
    /// <param name="cancellationToken">Token to cancel the sorting operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when sorting is already in progress or configuration is invalid.</exception>
    public async Task StartSortingAsync(bool dryRun = false, CancellationToken cancellationToken = default)
    {
        lock (_lockObject)
        {
            if (_isSorting)
                throw new InvalidOperationException("Sorting is already in progress.");

            _isSorting = true;
        }

        try
        {
            Validate();

            OnSortingStarted(new ImageSorterEventArgs("Sorting started.", 0));

            var imageFiles = GetImageFiles();

            if (imageFiles.Count == 0)
            {
                OnSortingCompleted(new ImageSorterEventArgs("No images found to sort.", 100));
                return;
            }

            System.IO.Directory.CreateDirectory(Target);

            for (int i = 0; i < imageFiles.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var newPath = await ProcessImageAsync(imageFiles[i], dryRun);
                    OnImageSorted(new ImageSorterEventArgs(
                        $"Sorted: {imageFiles[i]} -> {newPath}", 
                        (int)((i + 1) / (double)imageFiles.Count * 100), 
                        imageFiles[i],
                        newPath));
                }
                catch (Exception ex)
                {
                    OnErrorOccurred(new ImageSorterEventArgs(
                        $"Error processing {imageFiles[i]}: {ex.Message}", 
                        (int)((i + 1) / (double)imageFiles.Count * 100), 
                        imageFiles[i]));
                }

                int progress = (int)((i + 1) / (double)imageFiles.Count * 100);
                OnSortingProgressUpdated(new ImageSorterEventArgs($"Progress: {progress}%", progress));
            }

            OnSortingCompleted(new ImageSorterEventArgs("Sorting completed successfully.", 100));
        }
        catch (OperationCanceledException)
        {
            OnErrorOccurred(new ImageSorterEventArgs("Sorting was cancelled.", 0));
        }
        catch (Exception ex)
        {
            OnErrorOccurred(new ImageSorterEventArgs($"An error occurred: {ex.Message}", 0));
        }
        finally
        {
            lock (_lockObject)
            {
                _isSorting = false;
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>Gets all image files from the source directory.</summary>
    private List<string> GetImageFiles()
    {
        return System.IO.Directory.GetFiles(Source, "*.*", SearchOption.AllDirectories)
            .Where(f => IsSupportedImageFile(f))
            .ToList();
    }

    /// <summary>Determines if a file is a supported image format.</summary>
    private static bool IsSupportedImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedImageExtensions.Contains(extension);
    }

    /// <summary>Processes a single image file and returns the new path.</summary>
    private async Task<string> ProcessImageAsync(string imagePath, bool dryRun)
    {
        var targetPath = CalculateTargetPath(imagePath);
        var targetDirectory = Path.GetDirectoryName(targetPath);

        if (string.IsNullOrEmpty(targetDirectory))
            throw new InvalidOperationException($"Could not determine target directory for {imagePath}");

        if (!dryRun)
        {
            System.IO.Directory.CreateDirectory(targetDirectory);

            if (File.Exists(targetPath) && !Overwrite)
            {
                targetPath = GenerateUniqueFilePath(targetPath);
            }

            // Use Copy if KeepOriginal is true, otherwise Move
            if (KeepOriginal)
            {
                await Task.Run(() => File.Copy(imagePath, targetPath, Overwrite));
            }
            else
            {
                await Task.Run(() => File.Move(imagePath, targetPath, Overwrite));
            }
        }

        return targetPath;
    }

    /// <summary>Calculates the target path for an image based on configuration.</summary>
    private string CalculateTargetPath(string imagePath)
    {
        var fileName = Rename ? GenerateNewFileName(imagePath) : Path.GetFileName(imagePath);
        var subDirectory = SortBy.ToLowerInvariant() switch
        {
            "date" => GetDateBasedStructure(imagePath),
            "name" => GetNameBasedStructure(imagePath),
            "size" => GetSizeBasedStructure(imagePath),
            _ => string.Empty
        };

        return Path.Combine(Target, subDirectory, fileName);
    }

    /// <summary>Gets the date-based directory structure.</summary>
    private string GetDateBasedStructure(string imagePath)
    {
        var dateTaken = GetImageDateTaken(imagePath);
        var translatedStructure = TranslateStructure(Structure);
        return dateTaken.ToString(translatedStructure);
    }

    /// <summary>Translates human-readable structure tokens to .NET format strings.</summary>
    private string TranslateStructure(string structure)
    {
        if (string.IsNullOrEmpty(structure))
            return string.Empty;

        var result = structure;

        // Define all replacements in order (longest first to avoid partial replacements)
        var replacements = new (string token, string format)[]
        {
            // Year
            ("YEAR", "yyyy"),
            ("YYYY", "yyyy"),
            ("YY", "yy"),
            
            // Month
            ("MONTH", "MMMM"),
            ("MMMM", "MMMM"),
            ("MMM", "MMM"),
            ("MONTHNUM", "MM"),
            ("MM", "MM"),
            ("M", "M"),
            
            // Day
            ("DAY", "dddd"),
            ("DDDD", "dddd"),
            ("DDD", "ddd"),
            ("DAYNUM", "dd"),
            ("DD", "dd"),
            ("D", "d"),
            
            // Hour
            ("HOUR", "HH"),
            ("HH", "HH"),
            ("H", "H"),
            
            // Minute
            ("MINUTE", "mm"),
            ("mm", "mm"),
            ("m", "m"),
            
            // Second
            ("SECOND", "ss"),
            ("SS", "ss"),
            ("S", "s")
        };

        // Process replacements in order (longest tokens first)
        foreach (var (token, format) in replacements.OrderByDescending(x => x.token.Length))
        {
            result = ReplaceTokenWithWordBoundary(result, token, format);
        }

        return result;
    }

    /// <summary>Replaces a token with its format string, respecting word boundaries.</summary>
    private string ReplaceTokenWithWordBoundary(string input, string token, string format)
    {
        var output = new System.Text.StringBuilder();
        var lastIndex = 0;

        for (int i = 0; i < input.Length; i++)
        {
            // Check if we found the token at this position
            if (i + token.Length <= input.Length && 
                input.Substring(i, token.Length) == token)
            {
                // Check word boundaries (not preceded or followed by alphanumeric)
                bool isStartBoundary = (i == 0 || !char.IsLetterOrDigit(input[i - 1]));
                bool isEndBoundary = (i + token.Length >= input.Length || !char.IsLetterOrDigit(input[i + token.Length]));

                if (isStartBoundary && isEndBoundary)
                {
                    // Found a match with word boundaries
                    output.Append(input.Substring(lastIndex, i - lastIndex));
                    output.Append(format);
                    lastIndex = i + token.Length;
                    i = lastIndex - 1;
                }
            }
        }

        output.Append(input.Substring(lastIndex));
        return output.ToString();
    }

    /// <summary>Gets the name-based directory structure.</summary>
    private string GetNameBasedStructure(string imagePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(imagePath);
        return fileName.Length > 0 ? fileName[0].ToString().ToUpper() : "Other";
    }

    /// <summary>Gets the size-based directory structure.</summary>
    private string GetSizeBasedStructure(string imagePath)
    {
        var fileInfo = new FileInfo(imagePath);
        return fileInfo.Length switch
        {
            < 1_000_000 => "Small",
            < 10_000_000 => "Medium",
            _ => "Large"
        };
    }

    /// <summary>Extracts the date taken from image metadata with fallback to file system dates.</summary>
    private DateTime GetImageDateTaken(string imagePath)
    {
        // Try to extract EXIF date from metadata
        var exifDate = TryGetExifDate(imagePath);
        
        // Validate the EXIF date - check if it's a suspicious/reset timestamp
        if (exifDate > DateTime.MinValue && IsValidExifDate(exifDate))
        {
            return exifDate;
        }

        // Fallback to earliest file system date
        return GetEarliestFileSystemDate(imagePath);
    }

    /// <summary>Validates whether an EXIF date is likely genuine or a reset/default timestamp.</summary>
    private bool IsValidExifDate(DateTime exifDate)
    {
        // Check 1: EXIF date is in the future (impossible)
        if (exifDate > DateTime.Now)
        {
            System.Diagnostics.Debug.WriteLine($"EXIF date is in the future: {exifDate:yyyy-MM-dd HH:mm:ss}");
            return false;
        }

        // Check 2: Common reset timestamps (camera default dates)
        // Many cameras reset to these dates when battery dies or is replaced
        var suspiciousDateTimes = new[]
        {
            new DateTime(1970, 1, 1),   // Unix epoch
            new DateTime(1980, 1, 1),   // Some cameras
            new DateTime(2000, 1, 1),   // Very common reset date
            new DateTime(2010, 1, 1),   // Some newer cameras
            new DateTime(2020, 1, 1),   // Recent cameras
        };

        foreach (var suspiciousDate in suspiciousDateTimes)
        {
            // Check if EXIF date matches the suspicious date (allowing for time differences)
            if (exifDate.Year == suspiciousDate.Year &&
                exifDate.Month == suspiciousDate.Month &&
                exifDate.Day == suspiciousDate.Day)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"EXIF date appears to be a camera reset timestamp: {exifDate:yyyy-MM-dd HH:mm:ss}");
                return false;
            }
        }

        // Check 3: Very old dates (before digital cameras were common)
        if (exifDate.Year < 1995)
        {
            System.Diagnostics.Debug.WriteLine(
                $"EXIF date is suspiciously old: {exifDate:yyyy-MM-dd HH:mm:ss}");
            return false;
        }

        return true;
    }

    /// <summary>Attempts to extract the date taken from image EXIF metadata.</summary>
    private DateTime TryGetExifDate(string imagePath)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(imagePath);

            // Try to get DateTimeOriginal from SubIFD (most accurate for photos)
            var subIfdDir = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdDir != null)
            {
                if (subIfdDir.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var originalDateTime))
                {
                    if (originalDateTime > DateTime.MinValue)
                        return originalDateTime;
                }

                // Try DateTimeDigitized from SubIFD
                if (subIfdDir.TryGetDateTime(ExifDirectoryBase.TagDateTimeDigitized, out var digitizedDateTime))
                {
                    if (digitizedDateTime > DateTime.MinValue)
                        return digitizedDateTime;
                }
            }

            // Try DateTime from IFD0 (fallback)
            var exifDir = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (exifDir != null)
            {
                if (exifDir.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
                {
                    if (dateTime > DateTime.MinValue)
                        return dateTime;
                }
            }

            return DateTime.MinValue;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read EXIF from {imagePath}: {ex.Message}");
            return DateTime.MinValue;
        }
    }

    /// <summary>Gets the earliest date from file system metadata (creation or last write time).</summary>
    private DateTime GetEarliestFileSystemDate(string imagePath)
    {
        try
        {
            var creationTime = File.GetCreationTime(imagePath);
            var lastWriteTime = File.GetLastWriteTime(imagePath);

            // Return the earliest date
            var earliestDate = lastWriteTime < creationTime ? lastWriteTime : creationTime;
            
            // Ensure we never return MinValue
            if (earliestDate == DateTime.MinValue)
            {
                return DateTime.Now;
            }

            return earliestDate;
        }
        catch
        {
            return DateTime.Now;
        }
    }

    /// <summary>Determines if a file is a RAW image format.</summary>
    private static bool IsRawFile(string extension)
    {
        return extension is ".nef" or ".cr2" or ".arw" or ".dng" or ".raw";
    }

    /// <summary>Generates a new file name for the image based on its date taken.</summary>
    private string GenerateNewFileName(string imagePath)
    {
        var extension = Path.GetExtension(imagePath);
        var dateTaken = GetImageDateTaken(imagePath);
        return $"{dateTaken:yyyyMMdd_HHmmss}{extension}";
    }

    /// <summary>Generates a unique file path if the target already exists.</summary>
    private string GenerateUniqueFilePath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        int counter = 1;
        string newPath;

        do
        {
            newPath = Path.Combine(directory ?? string.Empty, $"{fileName}_{counter}{extension}");
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    #endregion

    #region Event Handlers

    protected virtual void OnSortingStarted(ImageSorterEventArgs e) => SortingStarted?.Invoke(this, e);
    protected virtual void OnSortingCompleted(ImageSorterEventArgs e) => SortingCompleted?.Invoke(this, e);
    protected virtual void OnImageSorted(ImageSorterEventArgs e) => ImageSorted?.Invoke(this, e);
    protected virtual void OnErrorOccurred(ImageSorterEventArgs e) => ErrorOccurred?.Invoke(this, e);
    protected virtual void OnSortingProgressUpdated(ImageSorterEventArgs e) => SortingProgressUpdated?.Invoke(this, e);

    #endregion
}
