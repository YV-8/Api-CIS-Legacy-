package com.user.management.dtos;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class UserResponseDTO {
    private String id;

    @NotBlank(message = "Name is required")
    @Size(min = 3, max = 200, message = "Name must be between 3 and 200 characters")
    private String name;

    @NotBlank(message = "Login is required")
    @Size(min = 3, max = 20, message = "Login must be between 3 and 20 characters")
    private String login;

    @NotBlank(message = "Password is required")
    private String password;        

}
